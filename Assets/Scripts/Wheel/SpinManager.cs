using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpinManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform _wheelTransform;
    [SerializeField] Button _spinButton;
    [SerializeField] WheelRewards _rewards;

    [Header("Wheel Settings")]
    [SerializeField] float _spinSpeed = 600f;
    [SerializeField] int _segmentCount = 8;
    [SerializeField] float _offsetDeg = 0f;
    [Range(0f, 1f)][SerializeField] float _landingBias = 0.5f;
    [SerializeField] int _minLaps = 1;

    [Header("Duration (soft)")]
    [SerializeField] float _baseDuration = 12f;
    [SerializeField] float _durationJitter = 1.0f;

    [SerializeField] MonoBehaviour _randomProviderBehaviour;
    [SerializeField] MonoBehaviour _wheelAnimatorBehaviour;

    IRandomIndexProvider _random;
    IWheelAnimator _anim;
    IRewardApplier _rewardApplier;

    bool _isSpinning;
    float _currentSpeed;
    int _targetIndex;

    public event Action<int> SpinLanded;
    public event Action SpinStarted;
    public event Action SpinFinished;
    public event Action RewardWon;

    void Awake()
    {
        if (_spinButton)
        {
            _spinButton.interactable = false;
        }

        _random = (_randomProviderBehaviour as IRandomIndexProvider) ?? new RandomNumberApiProvider();
        _anim = (_wheelAnimatorBehaviour as IWheelAnimator) ?? new WheelAnimator();
        _rewardApplier = new FirebaseRewardApplier();
    }

    void Start()
    {
        _spinButton.onClick.AddListener(OnSpinButton);
    }

    void OnDestroy()
    {
        _spinButton.onClick.RemoveListener(OnSpinButton);
    }

    void OnSpinButton()
    {
        if (_isSpinning) return;
        StartCoroutine(OnSpinButtonRoutine());
    }

    IEnumerator OnSpinButtonRoutine()
    {
        if (FirebaseServices.Instance && !FirebaseServices.Instance.OnlineMode)
        {
            Debug.LogWarning("When you are not online, the spin result will not be written to the server.");
        }

        if (FirebaseServices.Instance && FirebaseServices.Instance.OnlineMode)
        {
            var uid = FirebaseServices.Instance.UserId;
            var getTask = FirebaseServices.Instance.WalletRepo.GetAsync(uid);

            yield return CoroutineTasks.Wait(getTask);

            var data = getTask.Result;
            if (data != null)
            {
                var now = DateTime.UtcNow;
                var end = data.CooldownEndTime.ToDateTime();
                var remain = end > now ? (end - now) : TimeSpan.Zero;

                if (remain > TimeSpan.Zero)
                {
                    Debug.Log($"Cooldown: {remain:mm\\:ss} kaldı");
                    yield break; 
                }
            }
        }

        _isSpinning = true;
        _spinButton.interactable = false;
        SpinStarted?.Invoke();
        yield return StartCoroutine(SpinFlow());
    }


    IEnumerator SpinFlow()
    {
        _currentSpeed = _spinSpeed;
        var forever = _anim.SpinForever(this, _wheelTransform, () => _currentSpeed);

        var rndTask = _random.GetIndexAsync(_segmentCount);
        yield return CoroutineTasks.Wait(rndTask);
        _targetIndex = rndTask.IsCompletedSuccessfully ? rndTask.Result : 0;
        Debug.Log($"[SpinManager] Target index → {_targetIndex}");

        float duration = Mathf.Max(0.75f,
            UnityEngine.Random.Range(_baseDuration - _durationJitter, _baseDuration + _durationJitter));

        if (forever != null) StopCoroutine(forever);
        yield return _anim.EaseToIndexCW(_wheelTransform, _targetIndex, _segmentCount,
                                         _offsetDeg, _landingBias, _spinSpeed, _minLaps, duration);

        SpinLanded?.Invoke(_targetIndex);

        var slot = _rewards.GetSlotByIndex(_targetIndex);
        if (slot != null && FirebaseServices.Instance)
        {
            var user = FirebaseServices.Instance.UserId;
            if (!string.IsNullOrEmpty(user))
            {
                RewardWon?.Invoke();

                var applyTask = _rewardApplier.ApplyAsync(user, slot.CurrencyKey, slot.Amount, _targetIndex);
                yield return CoroutineTasks.Wait(applyTask);
                Debug.Log($"Won: {slot.GetDisplayText()} {slot.CurrencyKey}");

                yield return new WaitForSeconds(3f);    
                FinishSpin();
                yield break;
            }
        }

        if (!FirebaseServices.Instance.OnlineMode)
        {
            FinishSpin();
        }
    }

    void FinishSpin()
    {
        _isSpinning = false;
        _spinButton.interactable = true;
        SpinFinished?.Invoke();
    }
}