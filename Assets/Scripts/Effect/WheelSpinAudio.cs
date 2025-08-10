using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WheelSpinAudio : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] SpinManager _spinManager;
    [SerializeField] AudioClip _spinClip;     

    [Header("Speed → Audio")]
    [SerializeField] float _minSpeed = 3f;        
    [SerializeField] float _maxSpeed = 900f;       
    [SerializeField] float _maxVolume = 1f;
    [SerializeField] float _minPitch = 0.95f;
    [SerializeField] float _maxPitch = 1.15f;

    [Header("Smoothing")]
    [SerializeField] float _attack = 0.06f;
    [SerializeField] float _release = 0.15f;

    AudioSource _src;
    float _prevDeg;
    bool _spinning;
    RectTransform _wheel;

    void Reset()
    {           
        var a = GetComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = true;
        a.spatialBlend = 0f;
        a.dopplerLevel = 0f;
    }

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _wheel = GetComponent<RectTransform>();
        _src.playOnAwake = false;  
        _src.loop = true;
        _src.volume = 0f;
        _src.Stop();                
        if (_spinClip) _src.clip = _spinClip;
    }

    void OnEnable()
    {
        if (_wheel) _prevDeg = _wheel.eulerAngles.z;
        if (_spinManager != null)
        {
            _spinManager.SpinStarted += OnSpinStarted;
            _spinManager.SpinFinished += OnSpinFinished;
        }
    }

    void OnDisable()
    {
        if (_spinManager != null)
        {
            _spinManager.SpinStarted -= OnSpinStarted;
            _spinManager.SpinFinished -= OnSpinFinished;
        }
        if (_src) { _src.Stop(); _src.volume = 0f; }
        _spinning = false;
    }

    void OnSpinStarted()
    {
        _spinning = true;
        if (_src && _spinClip && !_src.isPlaying)
        {
            _src.volume = 0f;
            _src.pitch = 1f;
            _src.Play();
        }
    }

    void OnSpinFinished()
    {
        _spinning = false; 
    }

    void Update()
    {
        if (!_wheel || !_src || !_src.isPlaying) return;

        float curr = _wheel.eulerAngles.z;
        float dDeg = Mathf.DeltaAngle(_prevDeg, curr);
        _prevDeg = curr;

        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float speed = Mathf.Abs(dDeg) / dt;              
        float t = Mathf.InverseLerp(_minSpeed, _maxSpeed, speed);

        float tgtVol = _spinning ? Mathf.Lerp(0f, _maxVolume, t) : 0f;
        float tgtPit = Mathf.Lerp(_minPitch, _maxPitch, t);

        _src.volume = Smooth(_src.volume, tgtVol, _spinning ? _attack : _release);
        _src.pitch = tgtPit;

        if (!_spinning && _src.volume <= 0.001f)
            _src.Stop();
    }

    float Smooth(float from, float to, float timeConst)
    {
        float a = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-3f, timeConst));
        return Mathf.Lerp(from, to, a);
    }
}