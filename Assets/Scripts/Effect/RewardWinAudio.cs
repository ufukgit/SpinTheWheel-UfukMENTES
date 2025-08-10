using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RewardWinAudio : MonoBehaviour
{
    [SerializeField] SpinManager _spinManager;  
    [SerializeField] AudioClip _rewardClip;   

    [SerializeField, Range(0f, 1f)] float _volume = 1f;
    [SerializeField, Range(0f, 0.2f)] float _pitchJitter = 0.04f;

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;   
        src.dopplerLevel = 0f;
        src.Stop();
    }

    void OnEnable()
    {
        if (!_spinManager) return;

        _spinManager.RewardWon += OnGrantFinished;
    }

    void OnDisable()
    {
        if (!_spinManager) return;

        _spinManager.RewardWon -= OnGrantFinished;
    }

    void OnGrantFinished()
    {
        PlayOnce();
    }

    void PlayOnce()
    {
        if (!_rewardClip || !src) return;
        src.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
        src.PlayOneShot(_rewardClip, _volume);
    }
}