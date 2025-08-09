using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PointerFollower : MonoBehaviour
{
    public RectTransform wheelTransform;

    public float springStrength = 80f;
    public float damping = 8f;
    public float impulseStrength = 0.02f;
    public float maxSwingAngle = 45f;

    public int segmentCount = 8;

    float _angle = 0f;               
    float _velocity = 0f;            
    float _prevFrameAngle;           
    float _prevUnwrappedAngle;       

    void Start()
    {
        _prevFrameAngle = wheelTransform.eulerAngles.z;
        _prevUnwrappedAngle = _prevFrameAngle;
    }
    void Update()
    {
        float dt = Time.deltaTime;
        float currFrameAngle = wheelTransform.eulerAngles.z;
        float delta = Mathf.DeltaAngle(_prevFrameAngle, currFrameAngle);
        float currUnwrapped = _prevUnwrappedAngle + delta;
        float segmentSize = 360f / segmentCount;
        float prevIndex = Mathf.Floor(_prevUnwrappedAngle / segmentSize);
        float currIndex = Mathf.Floor(currUnwrapped / segmentSize);
        if (currIndex != prevIndex)
        {
            float wheelSpeed = delta / dt;
            _velocity += Mathf.Abs(wheelSpeed) * impulseStrength;
        }

        float target = -currFrameAngle;
        float diff = Mathf.DeltaAngle(_angle, target);
        float springForce = diff * springStrength;
        float dampForce = -_velocity * damping;

        _velocity += (springForce + dampForce) * dt;
        _angle += _velocity * dt;

        if (_angle > 0f)
        {
            _angle = 0f;
            _velocity = -_velocity * 0.5f;
        }
        else if (_angle < -maxSwingAngle)
        {
            _angle = -maxSwingAngle;
            _velocity = -_velocity * 0.5f;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, _angle);

        _prevFrameAngle = currFrameAngle;
        _prevUnwrappedAngle = currUnwrapped;
    }
}