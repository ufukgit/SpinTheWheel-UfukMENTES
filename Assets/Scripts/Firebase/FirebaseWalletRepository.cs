using Firebase;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class FirebaseWalletRepository : IWalletRepository
{
    readonly FirebaseFirestore _db;
    public FirebaseWalletRepository(FirebaseFirestore db) => _db = db;

    DocumentReference UserDoc(string uid) => _db.Collection("users").Document(uid);

    Dictionary<string, long> EmptyBalances()
        => new() { { "Money", 0 }, { "Gem", 0 } };

    public async Task EnsureUserAsync(string uid)
    {
        var doc = UserDoc(uid);
        var snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            var now = DateTime.UtcNow;
            var initial = new WalletData
            {
                Balances = EmptyBalances(),
                SpinCount = 0,
                CooldownEndTime = Timestamp.FromDateTime(now),
                LastSpinIndex = 0,
                UpdatedAt = Timestamp.FromDateTime(now)
            };
            await doc.SetAsync(initial);
        }
        else
        {
            var data = snap.ConvertTo<WalletData>();
            data.Balances ??= new();
            bool changed = false;
            foreach (var k in EmptyBalances().Keys)
                if (!data.Balances.ContainsKey(k)) { data.Balances[k] = 0; changed = true; }
            if (changed) await doc.UpdateAsync("Balances", data.Balances);
        }
    }

    public async Task<WalletData> GetAsync(string uid)
    {
        var snap = await UserDoc(uid).GetSnapshotAsync();
        return snap.Exists ? snap.ConvertTo<WalletData>() : null;
    }

    public async Task ApplyRewardAsync(string uid, string currencyKey, long amountUnits, int landedIndex,
                                       ICooldownPolicy policy, string spinId)
    {
        var userDoc = UserDoc(uid);
        var ledgerRef = userDoc.Collection("ledger").Document(spinId);

        int attempts = 0;
        while (true)
        {
            try
            {
                await _db.RunTransactionAsync(async tx =>
                {
                    if ((await tx.GetSnapshotAsync(ledgerRef)).Exists) return;

                    var snap = await tx.GetSnapshotAsync(userDoc);
                    var data = snap.Exists ? snap.ConvertTo<WalletData>() : new WalletData { Balances = EmptyBalances() };

                    var now = DateTime.UtcNow;
                    data.Balances ??= new();
                    if (!data.Balances.ContainsKey(currencyKey)) data.Balances[currencyKey] = 0;
                    data.Balances[currencyKey] += amountUnits;

                    data.SpinCount += 1;
                    (var endUtc, var minutes) = policy.ComputeNext(data.SpinCount, now);
                    data.CooldownEndTime = Timestamp.FromDateTime(endUtc);
                    data.LastSpinIndex = landedIndex;
                    data.UpdatedAt = Timestamp.FromDateTime(now);

                    tx.Set(userDoc, data);
                    tx.Set(ledgerRef, new
                    {
                        at = Timestamp.FromDateTime(now),
                        currency = currencyKey,
                        amount = amountUnits,
                        index = landedIndex,
                        spinId = spinId
                    });
                });
                return;
            }
            catch (FirebaseException ex) when (attempts < 2) 
            {
                attempts++;
                await Task.Delay(200 * (int)Mathf.Pow(2, attempts)); 
                Debug.LogWarning($"Retry tx (attempt {attempts}): {ex.Message}");
                continue;
            }
        }
    }
}