using Firebase.Firestore;
using System;
using System.Collections.Generic;

[FirestoreData]
public class WalletData
{
    [FirestoreProperty] public Dictionary<string, long> Balances { get; set; } = new();
    [FirestoreProperty] public int SpinCount { get; set; } = 0;
    [FirestoreProperty] public Timestamp CooldownEndTime { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
    [FirestoreProperty] public int LastSpinIndex { get; set; } = 0;
    [FirestoreProperty] public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
}