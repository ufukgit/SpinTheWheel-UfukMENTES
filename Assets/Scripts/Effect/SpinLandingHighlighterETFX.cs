using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpinLandingHighlighterETFX : MonoBehaviour
{
    [SerializeField] SpinManager _spin;
    [SerializeField] RectTransform[] _segmentAnchors;
    [SerializeField] DOTweenEffect _fx;

    [SerializeField] float _pulseScale = 1.25f;
    [SerializeField] float _pulseTime = 0.30f;
    [SerializeField] int _pulseCount = 3;

    [SerializeField] RectTransform _overlayLayer;
    [SerializeField] float _centerScale = 1.8f;
    [SerializeField] float _centerHold = 0.35f;

    void OnEnable()
    {
        if (_spin != null)
            _spin.SpinLanded += OnSpinLanded;
    }

    void OnDisable()
    {
        if (_spin != null)
            _spin.SpinLanded -= OnSpinLanded;
    }

    void OnSpinLanded(int index)
    {
        if (_segmentAnchors == null || _segmentAnchors.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, _segmentAnchors.Length - 1);

        var anchor = _segmentAnchors[index];
        if (anchor == null)
            return;

        StartCoroutine(Pulse(anchor));
    }

    IEnumerator Pulse(RectTransform target)
    {
        Vector3 baseScale = target.localScale;

        for (int i = 0; i < _pulseCount; i++)
        {
            bool isLast = (i == _pulseCount - 1) && _overlayLayer != null;

            if (isLast)
            {
                yield return StartCoroutine(MoveToCenterPulseAndBack(target, baseScale));
            }
            else
            {
                yield return StartCoroutine(SimplePulse(target, baseScale));
            }
        }

        target.localScale = baseScale;
    }

    private void PlayDOTweenEffect(RectTransform target)
    {
        var overlayCanvas = _overlayLayer ? _overlayLayer.GetComponentInParent<Canvas>() : null;
        Camera cam = (overlayCanvas && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                     ? overlayCanvas.worldCamera : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _overlayLayer,
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
            cam,
            out var centerLocal
        );

        var targetImage = target.GetComponent<Image>();
        if (_fx != null && targetImage != null)
            _fx.Play(centerLocal, targetImage);
    }

    IEnumerator SimplePulse(RectTransform target, Vector3 baseScale)
    {
        float t = 0f;
        while (t < _pulseTime)
        {
            t += Time.deltaTime;
            float k = t / _pulseTime;
            target.localScale = Vector3.Lerp(baseScale, baseScale * _pulseScale, k);
            yield return null;
        }
        t = 0f;
        while (t < _pulseTime)
        {
            t += Time.deltaTime;
            float k = t / _pulseTime;
            target.localScale = Vector3.Lerp(baseScale * _pulseScale, baseScale, k);
            yield return null;
        }
    }

    IEnumerator MoveToCenterPulseAndBack(RectTransform target, Vector3 baseScale)
    {
        var originalParent = target.parent as RectTransform;
        int originalSibling = target.GetSiblingIndex();
        Vector2 originalPos = target.anchoredPosition;
        Vector3 originalScale = target.localScale;

        var overlayCanvas = _overlayLayer.GetComponentInParent<Canvas>();
        Camera cam = (overlayCanvas && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                     ? overlayCanvas.worldCamera : null;

        Vector2 fromLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _overlayLayer,
            RectTransformUtility.WorldToScreenPoint(cam, target.position),
            cam, out fromLocal
        );

        Vector2 centerLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _overlayLayer,
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
            cam, out centerLocal
        );

        target.SetParent(_overlayLayer, worldPositionStays: false);
        target.anchoredPosition = fromLocal;

        PlayDOTweenEffect(target);

        float moveTime = _pulseTime * 0.8f;

        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = t / moveTime;
            float e = EaseOutCubic(k);
            target.anchoredPosition = Vector2.Lerp(fromLocal, centerLocal, e);
            target.localScale = Vector3.Lerp(originalScale, originalScale * _centerScale, e);
            yield return null;
        }

        yield return new WaitForSeconds(_centerHold);

        t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = t / moveTime;
            float e = EaseInCubic(k);
            target.anchoredPosition = Vector2.Lerp(centerLocal, fromLocal, e);
            target.localScale = Vector3.Lerp(originalScale * _centerScale, originalScale, e);
            yield return null;
        }

        target.SetParent(originalParent, worldPositionStays: false);
        target.SetSiblingIndex(originalSibling);
        target.anchoredPosition = originalPos;
        target.localScale = originalScale;
    }

    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    static float EaseInCubic(float x) => x * x * x;
}