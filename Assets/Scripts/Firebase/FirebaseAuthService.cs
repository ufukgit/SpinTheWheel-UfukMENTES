using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine;

public interface IAuthService
{
    Task<string> SignInAnonymouslyAsync();
    FirebaseUser CurrentUser { get; }
}

public sealed class FirebaseAuthService : IAuthService
{
    private FirebaseAuth _auth;
    public FirebaseUser CurrentUser => _auth.CurrentUser;

    public FirebaseAuthService(FirebaseAuth auth) 
    { 
        _auth = auth; 
    }

    public async Task<string> SignInAnonymouslyAsync()
    {
        var res = await _auth.SignInAnonymouslyAsync();
        Debug.Log($"Auth OK: {res.User.UserId}");
        return res.User.UserId;
    }
}