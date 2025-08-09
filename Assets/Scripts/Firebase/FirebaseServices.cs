using System.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEngine;

public enum OnlineState { Connecting, Online, Offline, Error }

public sealed class FirebaseServices : SingletonBehaviour<FirebaseServices>
{
    public IAuthService AuthService { get; private set; }
    public IWalletRepository WalletRepo { get; private set; }
    public ICooldownPolicy CooldownPolicy { get; private set; }
    public SpinRewardService SpinService { get; private set; }
    public string UserId { get; private set; }
    public bool OnlineMode { get; private set; } = false;
    public event System.Action<OnlineState, string> OnlineStateChanged;

    void SetState(OnlineState s, string message = null)
    {
        OnlineMode = (s == OnlineState.Online);
        OnlineStateChanged?.Invoke(s, message);
    }

    async void Start()
    {
        SetState(OnlineState.Connecting, "Connecting…");

        var ok = await FirebaseBootstrap.Instance.InitializeAsync();
        if (!ok)
        {
            SetState(OnlineState.Offline, $"Online features are not ready");
            return;
        }

        try
        {
            AuthService = new FirebaseAuthService(FirebaseBootstrap.Instance.Auth);
            WalletRepo = new FirebaseWalletRepository(FirebaseBootstrap.Instance.Db);
            CooldownPolicy = new ResetEveryTenLinearCooldown();
            SpinService = new SpinRewardService(WalletRepo, CooldownPolicy);

            UserId = await AuthService.SignInAnonymouslyAsync();
            await WalletRepo.EnsureUserAsync(UserId);
            SetState(OnlineState.Online);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            SetState(OnlineState.Error, "Online services failed to start. Please try again.");
        }
    }
}