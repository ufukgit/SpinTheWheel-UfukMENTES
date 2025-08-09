using System;
using System.Collections;
using UnityEngine;

public interface IWheelAnimator
{
    Coroutine SpinForever(MonoBehaviour host, RectTransform wheel, Func<float> getSpeed);
    IEnumerator EaseToIndexCW(RectTransform wheel,
                              int index,
                              int segmentCount,
                              float offsetDeg,
                              float landingBias01,
                              float spinSpeedDegPerSec,
                              int minLaps,
                              float durationSec);
    float GetTargetDegForIndex(int index, int segmentCount, float offsetDeg, float landingBias01);
}