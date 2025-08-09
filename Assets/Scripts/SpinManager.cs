using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SpinManager : MonoBehaviour
{
    [SerializeField] RectTransform _wheelTransform;
    [SerializeField] Button _spinButton;
    [SerializeField] WheelRewards _rewards;

    [SerializeField] float _spinSpeed = 600f;
    [SerializeField] int _segmentCount = 8;

    [SerializeField] float _offsetDeg = 0f;
    [Range(0f, 1f)]
    [SerializeField] float _landingBias = 0.5f;

    [SerializeField] float _baseDuration = 12f;
    [SerializeField] float _durationJitter = 1.0f;

    [SerializeField] int _minLaps = 1;

    bool _isSpinning;
    float _currentSpeed;
    int _targetIndex;

    public event Action<int> SpinLanded;

    void Start() => _spinButton.onClick.AddListener(OnSpinButton);
    void OnDestroy() => _spinButton.onClick.RemoveListener(OnSpinButton);

    void OnSpinButton()
    {
        if (_isSpinning) return;
        _isSpinning = true;
        _spinButton.interactable = false;
        StartCoroutine(FetchRandomIndexAndSpin());
    }

    IEnumerator FetchRandomIndexAndSpin()
    {
        _currentSpeed = _spinSpeed;
        var forever = StartCoroutine(SpinForever());

        using (var www = UnityWebRequest.Get(
            "http://www.randomnumberapi.com/api/v1.0/random?min=0&max=8&count=1"))
        {
            yield return www.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
            {
                Debug.LogError("Random API error: " + www.error);
                StopCoroutine(forever);
                FinishSpin();
                yield break;
            }

            string cleaned = www.downloadHandler.text.Trim().Trim('[', ']');
            _targetIndex = int.TryParse(cleaned, out var ix) ? ix : 0;
            Debug.Log($"[SpinManager] Target index → {_targetIndex}");
        }

        float duration = Mathf.Max(0.75f,
            UnityEngine.Random.Range(_baseDuration - _durationJitter, _baseDuration + _durationJitter));

        StopCoroutine(forever);
        yield return StartCoroutine(EasedDecelerateToIndexCW(_targetIndex, duration));

        SpinLanded?.Invoke(_targetIndex);

        var slot = _rewards.GetSlotByIndex(_targetIndex);
        if (slot == null)
        {
            Debug.LogError($"Reward slot not found for index: {_targetIndex}");
        }
        else
        {
            var t = FirebaseManager.Instance.AddRewardAsync(slot.Currency, slot.Amount, slot.Scale, _targetIndex);
            yield return CoroutineTasks.Wait(t);
            Debug.Log($"Won: {slot.GetDisplayText()} {slot.CurrencyKey}");
        }

        FinishSpin();
    }

    IEnumerator SpinForever()
    {
        while (true)
        {
            _wheelTransform.Rotate(0f, 0f, -_currentSpeed * Time.deltaTime);
            yield return null;
        }
    }

    float GetTargetDegForIndex(int index)
    {
        float seg = 360f / _segmentCount;
        float centerBiasDeg = seg * _landingBias;
        float targetDeg = Mathf.Repeat(_offsetDeg + index * seg + centerBiasDeg, 360f);
        return targetDeg;
    }

    IEnumerator EasedDecelerateToIndexCW(int index, float duration)
    {
        float targetDeg = GetTargetDegForIndex(index);
        float startDeg = _wheelTransform.eulerAngles.z % 360f;
        float deltaCW = Mathf.Repeat(startDeg - targetDeg, 360f);

        float desiredTotal = duration * (_spinSpeed * 0.55f);
        int laps = Mathf.Max(_minLaps, Mathf.FloorToInt((desiredTotal - deltaCW) / 360f));
        float totalCW = Mathf.Max(360f * _minLaps + deltaCW, laps * 360f + deltaCW);

        float startUnwrapped = startDeg;
        float endUnwrapped = startDeg - totalCW;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float angle = Mathf.Lerp(startUnwrapped, endUnwrapped, eased);
            _wheelTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void FinishSpin()
    {
        _isSpinning = false;
        _spinButton.interactable = true;
    }
}


public static class CoroutineTasks
{
    public static IEnumerator Wait(Task task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) Debug.LogError(task.Exception);
    }
}