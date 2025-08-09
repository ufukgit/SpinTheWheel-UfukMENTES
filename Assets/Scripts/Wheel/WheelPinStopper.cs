using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WheelPinStopper : MonoBehaviour
{
    [SerializeField] 
    RectTransform _wheelTransform;

    [SerializeField]
    float _springStrength = 80f;
    [SerializeField]
    float _damping = 8f;
    [SerializeField]
    float _impulseStrength = 0.02f;
    [SerializeField]
    float _maxSwingAngle = 45f;

    public int segmentCount = 8;

    float _angle = 0f;               
    float _velocity = 0f;            
    float _prevFrameAngle;           
    float _prevUnwrappedAngle;       

    void Start()
    {
        _prevFrameAngle = _wheelTransform.eulerAngles.z;
        _prevUnwrappedAngle = _prevFrameAngle;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float currFrameAngle = _wheelTransform.eulerAngles.z;
        float delta = Mathf.DeltaAngle(_prevFrameAngle, currFrameAngle);
        float currUnwrapped = _prevUnwrappedAngle + delta;
        float segmentSize = 360f / segmentCount;
        float prevIndex = Mathf.Floor(_prevUnwrappedAngle / segmentSize);
        float currIndex = Mathf.Floor(currUnwrapped / segmentSize);
        if (currIndex != prevIndex)
        {
            float wheelSpeed = delta / dt;
            _velocity += Mathf.Abs(wheelSpeed) * _impulseStrength;
        }

        float target = -currFrameAngle;
        float diff = Mathf.DeltaAngle(_angle, target);
        float springForce = diff * _springStrength;
        float dampForce = -_velocity * _damping;

        _velocity += (springForce + dampForce) * dt;
        _angle += _velocity * dt;

        if (_angle > 0f)
        {
            _angle = 0f;
            _velocity = -_velocity * 0.5f;
        }
        else if (_angle < -_maxSwingAngle)
        {
            _angle = -_maxSwingAngle;
            _velocity = -_velocity * 0.5f;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, _angle);

        _prevFrameAngle = currFrameAngle;
        _prevUnwrappedAngle = currUnwrapped;
    }
}