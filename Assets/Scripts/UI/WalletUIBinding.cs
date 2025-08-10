using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WalletUIBinding : MonoBehaviour
{
    [SerializeField] TMP_Text _moneyText;
    [SerializeField] TMP_Text _gemText;

    FirebaseServices _firebaseServices;

    readonly Dictionary<string, long> _balances = new Dictionary<string, long>
    {
        {"Money", 0},
        {"Gem",   0}
    };

    string _uid;

    void Start()
    {
        _firebaseServices = FirebaseServices.Instance;
        if (_firebaseServices != null)
        {
            _firebaseServices.UserIDChanged += OnUserIdChanged;
        }
    }

    void OnDestroy()
    {
        if (_firebaseServices != null)
        {
            _firebaseServices.UserIDChanged -= OnUserIdChanged;
            if (_firebaseServices.SpinService != null)
                _firebaseServices.SpinService.RewardApplied -= OnRewardApplied;
        }
    }

    void OnUserIdChanged(string uid)
    {
        if (_firebaseServices.SpinService != null)
            _firebaseServices.SpinService.RewardApplied += OnRewardApplied;

        _uid = uid;
        StopAllCoroutines();
        StartCoroutine(LoadThenRender());
    }

    IEnumerator LoadThenRender()
    {
        yield return LoadWalletData();
        Render();                      
    }

    IEnumerator LoadWalletData()
    {
        if (_firebaseServices == null || string.IsNullOrEmpty(_uid))
        {
            yield break;
        }

        var task = _firebaseServices.WalletRepo.GetAsync(_uid);
        yield return CoroutineTasks.Wait(task);
        var data = task.Result;

        if (data?.Balances != null)
        {
            _balances["Money"] = SafeGet(data.Balances, "Money");
            _balances["Gem"] = SafeGet(data.Balances, "Gem");
        }
        else
        {
            _balances["Money"] = 0;
            _balances["Gem"] = 0;
        }
    }

    void OnRewardApplied(string uid, string currencyKey, long amountUnits)
    {
        if (uid != _uid) return; 
        if (!_balances.ContainsKey(currencyKey)) _balances[currencyKey] = 0;

        _balances[currencyKey] += amountUnits;
        Render();
    }

    void Render()
    {
        if (_moneyText)
        {
            var money = _balances["Money"];
            var moneyFloat = (float)money / 100f;
            _moneyText.text = Format(moneyFloat);
        }

        if (_gemText)
        {
            _gemText.text = Format(_balances["Gem"]);
        }
    }

    static long SafeGet(Dictionary<string, long> d, string k)
        => (d != null && d.TryGetValue(k, out var v)) ? v : 0;

    static string Format(long v)
    {
        return v.ToString("N0");
    }
    static string Format(float v)
    {
        return v.ToString("N2");
    }
}