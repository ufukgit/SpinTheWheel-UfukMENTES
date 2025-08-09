
using Firebase.Firestore;

[FirestoreData]
public class LedgerEntry
{
    [FirestoreProperty] public Timestamp At { get; set; }
    [FirestoreProperty] public string Type { get; set; } = "spin";
    [FirestoreProperty] public string Currency { get; set; }
    [FirestoreProperty] public long Amount { get; set; }
    [FirestoreProperty] public int Index { get; set; }
    [FirestoreProperty] public string SpinId { get; set; } 
}