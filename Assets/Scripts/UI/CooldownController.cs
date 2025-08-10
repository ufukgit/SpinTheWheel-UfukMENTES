using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CooldownController : MonoBehaviour
{
    [SerializeField] Button _spinButton;
    [SerializeField] TMP_Text _countdownText;
    [SerializeField] TMP_Text _spinButtonLabel;
    [SerializeField] GameObject _coolDownObject;
    [SerializeField] SpinManager _spinManager;

    string _uid;
    bool _spinning;
    bool _lastAvailable;
    string _lastMsg = "";

    private void Start()
    {
        if (FirebaseServices.Instance != null)
        {
            FirebaseServices.Instance.OnlineStateChanged += HandleState;
            FirebaseServices.Instance.UserIDChanged += HandleUserID;
        }

        if (PendingSpinRecoverer.Instance != null)
        {
            PendingSpinRecoverer.Instance.RecoveryStarted += OnRecStart;
            PendingSpinRecoverer.Instance.RecoveryFinished += OnRecFinish;
        }

        if (_spinManager != null)
        {
            _spinManager.SpinStarted += OnSpinStarted;
            _spinManager.SpinFinished += OnSpinFinished;
        }

        StartCoroutine(RefreshLoop());
    }

    void OnDestroy()
    {
        if (FirebaseServices.Instance != null)
        {
            FirebaseServices.Instance.OnlineStateChanged -= HandleState;
            FirebaseServices.Instance.UserIDChanged -= HandleUserID;
        }

        if (PendingSpinRecoverer.Instance != null)
        {
            PendingSpinRecoverer.Instance.RecoveryStarted -= OnRecStart;
            PendingSpinRecoverer.Instance.RecoveryFinished -= OnRecFinish;
        }

        if (_spinManager != null)
        {
            _spinManager.SpinStarted -= OnSpinStarted;
            _spinManager.SpinFinished -= OnSpinFinished;
        }

        StopAllCoroutines();
    }

    void OnRecStart()
    {
        _lastAvailable = false;          
        ApplyUI();
    }

    void OnRecFinish(bool ok)
    {
        StartCoroutine(RefreshOnce());
    }

    private void HandleState(OnlineState state, string arg2)
    {
        if (state != OnlineState.Online && FirebaseServices.Instance.Retrying && _coolDownObject != null)
        {
            _coolDownObject.SetActive(false);
        }
    }

    void OnSpinStarted()
    {
        _spinning = true; ApplyUI();
    }

    void OnSpinFinished()
    {
        _spinning = false; ApplyUI();
    }

    private void HandleUserID(string strID)
    {
        _uid = strID;
    }

    IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return RefreshOnce();
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator RefreshOnce()
    {
        if (!(FirebaseServices.Instance && FirebaseServices.Instance.OnlineMode) || string.IsNullOrEmpty(_uid))
        {
            SetLogic(available: true, msg: "");
            yield break;
        }

        var task = FirebaseServices.Instance.WalletRepo.GetAsync(_uid);
        yield return CoroutineTasks.Wait(task);
        var data = task.Result;

        if (data == null) { SetLogic(true, ""); yield break; }

        var now = DateTime.UtcNow;
        var end = data.CooldownEndTime.ToDateTime();
        var remain = end > now ? (end - now) : TimeSpan.Zero;

        if (remain <= TimeSpan.Zero)
        {
            _coolDownObject.SetActive(false);
            SetLogic(true, "");
        }
        else
        {
            _coolDownObject.SetActive(true);
            SetLogic(false, $"{remain:mm\\:ss}");
        }
    }

    void SetLogic(bool available, string msg)
    {
        _lastAvailable = available;
        _lastMsg = msg;
        ApplyUI();
    }

    void ApplyUI()
    {
        var svc = FirebaseServices.Instance;

        bool hasPending = PendingSpinStore.TryLoad(out var p);
        bool recovering = PendingSpinRecoverer.Instance.IsRecovering;
        bool online = svc != null && svc.OnlineMode;
        bool enableButton = _lastAvailable
                            && !_spinning
                            && online
                            && !hasPending
                            && !recovering;

        if (_countdownText) _countdownText.text = _lastAvailable ? "" : _lastMsg;

        if (_spinButtonLabel)
            _spinButtonLabel.color = enableButton ? Color.white : ParseHex("8C8C8C");

        if (_spinButton && _spinButton.image)
            _spinButton.image.color = enableButton ? Color.white : ParseHex("3F3F3F");

        if (_spinButton)
        {
            var cb = _spinButton.colors;
            cb.normalColor = Color.white;
            cb.disabledColor = Color.white;
            _spinButton.colors = cb;

            _spinButton.interactable = enableButton;
        }
    }


    static Color ParseHex(string hex)
    {
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }


    public void TriggerImmediateRefresh() { StopAllCoroutines(); StartCoroutine(RefreshLoop()); }
}