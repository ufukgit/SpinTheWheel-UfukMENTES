using System;
using System.Threading.Tasks;
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
    public event System.Action<string> UserIDChanged;

    const int MaxAttempts = 5;
    const float MaxBackoffSec = 30f;
    bool _retrying;
    public bool Retrying => _retrying;

    void SetState(OnlineState s, string message = null)
    {
        OnlineMode = (s == OnlineState.Online);
        OnlineStateChanged?.Invoke(s, message);
    }

    async void Start()
    {
        await InitWithRetryAsync();
    }

    public async void RetryNow()
    {
        if (_retrying) return;
        await InitWithRetryAsync(startImmediate: true);
    }

    async Task InitWithRetryAsync(bool startImmediate = false)
    {
        if (_retrying) return;
        _retrying = true;

        float backoff = startImmediate ? 0f : 1f;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                SetState(OnlineState.Offline, "No internet");
                await Task.Delay(1000);
                attempt--; 
                continue;
            }

            SetState(OnlineState.Connecting, attempt == 1 ? "Connecting…" : "Reconnecting…");

            var ok = await TryInitOnceAsync();
            if (ok)
            {
                _retrying = false;
                return;
            }

            if (attempt < MaxAttempts)
            {
                SetState(OnlineState.Offline, $"Retrying… ({attempt}/{MaxAttempts})");
                await Task.Delay(TimeSpan.FromSeconds(backoff));
                backoff = Mathf.Min(backoff <= 0f ? 1f : backoff * 2f, MaxBackoffSec);
            }
        }

        SetState(OnlineState.Error, $"Online services failed after {MaxAttempts} attempts.");
        _retrying = false;
    }

    async Task<bool> TryInitOnceAsync()
    {
        try
        {
            var ok = await FirebaseBootstrap.Instance.InitializeAsync();
            if (!ok)
            {
                return false;
            }

            AuthService = new FirebaseAuthService(FirebaseBootstrap.Instance.Auth);
            WalletRepo = new FirebaseWalletRepository(FirebaseBootstrap.Instance.Db);
            CooldownPolicy = new ResetEveryTenLinearCooldown();
            SpinService = new SpinRewardService(WalletRepo, CooldownPolicy);

            UserId = await AuthService.SignInAnonymouslyAsync();
            UserIDChanged?.Invoke(UserId);

            await WalletRepo.EnsureUserAsync(UserId);

            SetState(OnlineState.Online, null);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FirebaseServices] Init failed: {e.Message}");
            return false;
        }
    }
}