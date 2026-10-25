using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyCombatPressure : MonoBehaviour
{
    private const float MinimumThresholdGap = 0.01f;

    [Header("pressure Gain")]
    [SerializeField, Range(0f, 1f)] private float pressurePerHit = 0.25f;

    [Header("Pressure Recovery")]
    [SerializeField, Min(0f)] private float decayDelay = 2f;
    [SerializeField, Min(0f)] private float decayPerSecond = 0.15f;

    [Header("High Pressure")]
    [SerializeField, Range(MinimumThresholdGap, 1f)] private float highPressureEnter = 0.65f;
    [SerializeField, Range(0f, 1f)] private float highPressureExit = 0.35f;

    private float _pressure;
    private float _lastHitTime;
    private float _lastUpdateTime;

    public float PressureNormalized => _pressure;
    public bool IsHighPressure { get; private set; }

    private float EnterThreshold => Mathf.Clamp(highPressureEnter, MinimumThresholdGap, 1f);
    private float ExitThreshold => Mathf.Clamp(highPressureExit , 0f, EnterThreshold -MinimumThresholdGap);

    private void OnEnable()
    {
        ResetPressure();
    }

    public void ResetPressure()
    {
        _pressure = 0f;
        _lastHitTime = float.NegativeInfinity;
        _lastUpdateTime = Time.time;
        IsHighPressure = false;
    }

    public void RegisterHit()
    {
        if (!isActiveAndEnabled)
            return;
        float now = Time.time;

        // 지난 시간의 회복을 먼저 반영하고 새 피격을 더함
        AdvanceDecay(now);

        _pressure = Mathf.Clamp01(_pressure + Mathf.Clamp01(pressurePerHit));

        _lastHitTime = now;

        RefreshHighPressureState();
    }

    public void TickPressure()
    {
        if(!isActiveAndEnabled)
            return;
        
        AdvanceDecay(Time.time);

        RefreshHighPressureState();
    }

    private void AdvanceDecay(float now)
    {
        float recoveryStart = _lastHitTime + Mathf.Max(0f, decayDelay);

        float decayStart = Mathf.Max(_lastUpdateTime, recoveryStart);

        float elapsed = Mathf.Max(0f, now - decayStart);

        _pressure = Mathf.MoveTowards(_pressure, 0f, Mathf.Max(0f, decayPerSecond) * elapsed);

        _lastUpdateTime = now;
    }

    private void RefreshHighPressureState()
    {
        if (IsHighPressure)
        {
            if(_pressure <= ExitThreshold)     
                IsHighPressure = false;     
        }


        else if(_pressure >= EnterThreshold)
            IsHighPressure = true;
    }

}
