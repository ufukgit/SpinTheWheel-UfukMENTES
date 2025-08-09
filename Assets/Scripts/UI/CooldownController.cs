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

    string _uid;

    private void Start()
    {
        if (FirebaseServices.Instance != null)
            FirebaseServices.Instance.UserIDChanged += HandleUserID;

        StartCoroutine(RefreshLoop());
    }

    void OnDestroy()
    {
        if (FirebaseServices.Instance != null)
            FirebaseServices.Instance.UserIDChanged -= HandleUserID;
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
            SetUI(available: true, msg: "");
            yield break;
        }

        var task = FirebaseServices.Instance.WalletRepo.GetAsync(_uid);
        yield return CoroutineTasks.Wait(task);
        var data = task.Result;
        if (data == null) { SetUI(true, ""); yield break; }

        var now = DateTime.UtcNow;
        var end = data.CooldownEndTime.ToDateTime();
        var remain = end > now ? (end - now) : TimeSpan.Zero;

        if (remain <= TimeSpan.Zero)
        {
            _coolDownObject.SetActive(false);
            SetUI(true, "");
        }
        else
        {
            _coolDownObject.SetActive(true);
            SetUI(false, $"{remain:mm\\:ss}");
        }
    }

    void SetUI(bool available, string msg)
    {
        if (_countdownText)
            _countdownText.text = available ? "" : msg;

        Color white = Color.white;
        Color labelDisabled = ParseHex("8C8C8C");
        Color buttonDisabled = ParseHex("3F3F3F");

        if (_spinButtonLabel)
            _spinButtonLabel.color = available ? white : labelDisabled;

        if (_spinButton && _spinButton.image)
            _spinButton.image.color = available ? white : buttonDisabled;

        if (_spinButton)
        {
            var cb = _spinButton.colors;
            cb.normalColor = white;
            cb.disabledColor = white;
            _spinButton.colors = cb;

            _spinButton.interactable = available;
        }
    }

    static Color ParseHex(string hex)
    {
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }


    public void TriggerImmediateRefresh() { StopAllCoroutines(); StartCoroutine(RefreshLoop()); }
}