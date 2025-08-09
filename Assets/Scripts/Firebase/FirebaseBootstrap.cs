using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using System.Threading.Tasks;

public sealed class FirebaseBootstrap : SingletonBehaviour<FirebaseBootstrap>
{
    public FirebaseAuth Auth { get; private set; }
    public FirebaseFirestore Db { get; private set; }
    public static bool IsReady { get; private set; }
    public static string NotReadyReason { get; private set; }

    public async Task<bool> InitializeAsync()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync(); //is firebase ready check..
        if (status != DependencyStatus.Available)
        {
            IsReady = false;
            NotReadyReason = status.ToString();
            Debug.LogError("Firebase deps not available: " + status);
            return false;
        }

        Auth = FirebaseAuth.DefaultInstance;
        Db = FirebaseFirestore.DefaultInstance;
        Db.Settings.PersistenceEnabled = true;

        IsReady = true;
        NotReadyReason = null;
        
        return true;
    }
}