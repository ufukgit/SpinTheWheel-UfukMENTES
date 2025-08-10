using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WheelPinStopper : MonoBehaviour
{
    [Header("Wheel")]
    [SerializeField] RectTransform _wheel;
    [SerializeField] int _segmentCount = 8;     
    [SerializeField] float _notchOffsetDeg = 0f;    

    [Header("Pin Hinge")]
    [SerializeField] float _maxSwing = 42f;       
    [SerializeField] float _spring = 60f;       
    [SerializeField] float _damping = 12f;      

    [Header("Impulses / Contact")]
    [SerializeField] float _baseImpulse = 180f; 
    [SerializeField] float _impulsePerDegSpeed = 0.25f; 
    [SerializeField] float _contactWindow = 8f;   
    [SerializeField] float _contactPress = 120f;  
    [SerializeField] float _restitution = 0.35f;

    float _angle; 
    float _vel;   
    float _prevWheelDeg;
    float _unwrappedDeg;

    void Awake()
    {
        _prevWheelDeg = _wheel.eulerAngles.z;
        _unwrappedDeg = _prevWheelDeg;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float curr = _wheel.eulerAngles.z;
        float d = Mathf.DeltaAngle(_prevWheelDeg, curr); 
        _unwrappedDeg += d;
        float omega = -d / dt;

        float seg = 360f / Mathf.Max(1, _segmentCount);

        int prevIdx = Mathf.FloorToInt((_unwrappedDeg - d - _notchOffsetDeg) / seg);
        int currIdx = Mathf.FloorToInt((_unwrappedDeg - _notchOffsetDeg) / seg);
        if (prevIdx != currIdx)
        {
            float impulse = _baseImpulse + _impulsePerDegSpeed * Mathf.Abs(omega);
            float dir = (omega >= 0f) ? -1f : +1f;   
            _vel += dir * impulse;
            // burada klik SFX/efekt çağırabilirsin
        }

        float nearestIdx = Mathf.Round((_unwrappedDeg - _notchOffsetDeg) / seg);
        float nearestDeg = nearestIdx * seg + _notchOffsetDeg;
        float dist = Mathf.Abs(Mathf.DeltaAngle(_unwrappedDeg, nearestDeg));
        if (dist <= _contactWindow)
        {
            float sign = (omega >= 0f) ? -1f : +1f; 
            float w = 1f - (dist / _contactWindow);
            _vel += sign * (_contactPress * w) * dt;
        }

        float acc = (-_spring * _angle) - (_damping * _vel);
        _vel += acc * dt;
        _angle += _vel * dt;

        if (_angle > 0f) { _angle = 0f; if (_vel > 0f) _vel = -_vel * _restitution; }
        if (_angle < -_maxSwing) { _angle = -_maxSwing; if (_vel < 0f) _vel = -_vel * _restitution; }

        transform.localRotation = Quaternion.Euler(0f, 0f, _angle);

        _prevWheelDeg = curr;
    }
}