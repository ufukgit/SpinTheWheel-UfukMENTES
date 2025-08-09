using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FirebaseManager : SingletonBehaviour<FirebaseManager>
{
    public FirebaseAuth Auth;
    public FirebaseUser User;
    public FirebaseFirestore Db;

    public DocumentReference UserDoc => Db.Collection("users").Document(User.UserId);

    async void Start()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError("Firebase init failed: " + status);
            return;
        }
        else
        {
            Debug.Log("Firebase initialized!");
        }

        Auth = FirebaseAuth.DefaultInstance;
        await SignInAnonymouslyAsync();

        Db = FirebaseFirestore.DefaultInstance;

        try
        {
            Db.Settings.PersistenceEnabled = true;
            Debug.Log("Firestore persistence enabled");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not set Firestore persistence at runtime: " + ex.Message);
        }

        await EnsureUserDocAsync();
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            var authResult = await Auth.SignInAnonymouslyAsync();
            User = authResult.User;
            Debug.Log($"Anonymous login successful! Anonymous user ID: {User.UserId}");
        }
        catch (Exception e)
        {
            Debug.LogError("Anonymous login failed: " + e);
        }
    }

    public async Task EnsureUserDocAsync()
    {
        DocumentSnapshot snap = null;
        try
        {
            snap = await UserDoc.GetSnapshotAsync(Source.Server);
            Debug.Log("Get snapshot successful from Server!");
        }
        catch
        {
            Debug.LogWarning("Server unreachable, trying cache…");
            try 
            { 
                snap = await UserDoc.GetSnapshotAsync(Source.Cache); 
            } 
            catch 
            {
            }
        }

        if (snap == null || !snap.Exists)
        {
            var now = DateTime.UtcNow;
            var initial = new WalletData
            {
                Balances = EmptyBalancesFromEnum(),
                SpinCount = 0,
                CooldownEndTime = Timestamp.FromDateTime(now),
                LastSpinIndex = 0,
                UpdatedAt = Timestamp.FromDateTime(now)
            };
            await UserDoc.SetAsync(initial);
            Debug.Log("Created initial user data");
            return;
        }

        try
        {
            var data = snap.ConvertTo<WalletData>();
            data.Balances ??= new Dictionary<string, long>();
            if (AddMissingBalancesFromEnum(data.Balances))
            {
                await UserDoc.UpdateAsync("Balances", data.Balances);
                Debug.Log("User balances updated with new enum keys.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not sync enum keys: " + ex.Message);
        }
    }

    public Task AddRewardAsync(RewardCurrency currency, long displayAmount, int scale, int landedIndex)
    {
        long units = checked(displayAmount * Math.Max(1, scale));
        return AddRewardUnitsAsync(currency, units, landedIndex);
    }

    private async Task AddRewardUnitsAsync(RewardCurrency currency, long amountUnits, int landedIndex)
    {
        string key = RewardCurrencyUtil.Key(currency);

        await Db.RunTransactionAsync(async tx =>
        {
            var snap = await tx.GetSnapshotAsync(UserDoc);

            WalletData data = snap.Exists
                ? snap.ConvertTo<WalletData>()
                : new WalletData
                {
                    Balances = EmptyBalancesFromEnum(),
                    SpinCount = 0,
                    CooldownEndTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    LastSpinIndex = 0,
                    UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
                };

            var now = DateTime.UtcNow;

            data.Balances ??= new Dictionary<string, long>();
            AddMissingBalancesFromEnum(data.Balances);

            if (!data.Balances.ContainsKey(key))
                data.Balances[key] = 0;

            data.Balances[key] += amountUnits;

            data.SpinCount += 1;

            int step = (data.SpinCount - 1) % 10;
            int minutes = 10 + 5 * step;
            data.CooldownEndTime = Timestamp.FromDateTime(now.AddMinutes(minutes));

            data.LastSpinIndex = landedIndex;
            data.UpdatedAt = Timestamp.FromDateTime(now);

            tx.Set(UserDoc, data);
        });
    }

    public async Task<WalletData> GetUserDataAsync()
    {
        var snap = await UserDoc.GetSnapshotAsync();
        return snap.Exists ? snap.ConvertTo<WalletData>() : null;
    }

    public bool IsSpinAvailable(WalletData data, out TimeSpan remain)
    {
        var now = DateTime.UtcNow;
        var end = data.CooldownEndTime.ToDateTime();
        remain = end > now ? (end - now) : TimeSpan.Zero;
        return remain <= TimeSpan.Zero;
    }

    private static Dictionary<string, long> EmptyBalancesFromEnum()
    {
        var dict = new Dictionary<string, long>();
        foreach (RewardCurrency c in Enum.GetValues(typeof(RewardCurrency)))
            dict[RewardCurrencyUtil.Key(c)] = 0;
        return dict;
    }

    private static bool AddMissingBalancesFromEnum(Dictionary<string, long> balances)
    {
        bool changed = false;
        foreach (RewardCurrency c in Enum.GetValues(typeof(RewardCurrency)))
        {
            var key = RewardCurrencyUtil.Key(c);
            if (!balances.ContainsKey(key))
            {
                balances[key] = 0;
                changed = true;
            }
        }
        return changed;
    }
}