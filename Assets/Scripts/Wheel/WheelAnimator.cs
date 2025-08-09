using System;
using System.Collections;
using UnityEngine;

public sealed class WheelAnimator : IWheelAnimator
{
    public Coroutine SpinForever(MonoBehaviour host, RectTransform wheel, Func<float> getSpeed)
        => host.StartCoroutine(SpinForeverCo(wheel, getSpeed));

    IEnumerator SpinForeverCo(RectTransform wheel, Func<float> getSpeed)
    {
        while (true)
        {
            var speed = getSpeed != null ? getSpeed() : 0f;
            wheel.Rotate(0f, 0f, -speed * Time.deltaTime);
            yield return null;
        }
    }

    public float GetTargetDegForIndex(int index, int segmentCount, float offsetDeg, float landingBias01)
    {
        float seg = 360f / Mathf.Max(1, segmentCount);
        float centerBiasDeg = seg * Mathf.Clamp01(landingBias01);
        return Mathf.Repeat(offsetDeg + index * seg + centerBiasDeg, 360f);
    }

    public IEnumerator EaseToIndexCW(RectTransform wheel,
                                     int index,
                                     int segmentCount,
                                     float offsetDeg,
                                     float landingBias01,
                                     float spinSpeedDegPerSec,
                                     int minLaps,
                                     float durationSec)
    {
        float targetDeg = GetTargetDegForIndex(index, segmentCount, offsetDeg, landingBias01);
        float startDeg = wheel.eulerAngles.z % 360f;
        float deltaCW = Mathf.Repeat(startDeg - targetDeg, 360f);

        float duration = Mathf.Max(0.75f, durationSec);
        float desiredTotal = duration * (spinSpeedDegPerSec * 0.55f);
        int laps = Mathf.Max(minLaps, Mathf.FloorToInt((desiredTotal - deltaCW) / 360f));
        float totalCW = Mathf.Max(360f * minLaps + deltaCW, laps * 360f + deltaCW);

        float startUnwrapped = startDeg;
        float endUnwrapped = startDeg - totalCW;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float angle = Mathf.Lerp(startUnwrapped, endUnwrapped, eased);
            wheel.localRotation = Quaternion.Euler(0f, 0f, angle);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}