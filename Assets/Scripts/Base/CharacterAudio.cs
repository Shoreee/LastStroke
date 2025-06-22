// CharacterAudio.cs
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public enum AudioEvent {
    FootStep,
    WolfRunGrass,
    WolfWalkGrass,
    WolfGrabPen,
    WolfPutdownPen,
    WolfMagicPainting,
    WolfGrabPenWalking,
    WolfJump,
    WolfDeath,
    WolfMagicClose,
    WolfEnhanceStart,
    RedHatBasketInhaling,
    RedHatBasketMoving,
    RedHatBasketTransform,
    RedHatGrabBasket,
    RedHatPutdownBasket,
    RedHatRunGrass,
    RedHatWalkGrass,
    RedHatJump,
    RedHatDeath,
    RedHatEnhanceStart
}

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PhotonView))]
public class CharacterAudio : MonoBehaviourPun {
    [Header("Wolf Sounds")]
    public AudioClip[] wolfRunGrass;
    public AudioClip[] wolfWalkGrass;
    public AudioClip wolfGrabPen;
    public AudioClip wolfPutdownPen;
    public AudioClip wolfMagicPainting;
    public AudioClip wolfGrabPenWalking;
    public AudioClip wolfJump;
    public AudioClip wolfDeath;
    public AudioClip wolfEnhanceStart;

    // 主 AudioSource，用于 PlayOneShot
    private AudioSource _mainSource;

    // 缓存临时源：只存那些需要淡出的事件
    private Dictionary<AudioEvent, AudioSource> _tempSources = new();

    // 淡出时长，可在 Inspector 调整
    [Tooltip("需要淡出时的音量消失时长")]
    public float fadeDuration = 0.3f;

    [Header("RedHat Boss Sounds")]
    public AudioClip redHatBasketInhaling;
    public AudioClip redHatBasketMoving;
    public AudioClip redHatBasketTransform;
    public AudioClip redHatGrabBasket;
    public AudioClip redHatPutdownBasket;
    public AudioClip[] redHatRunGrass;
    public AudioClip[] redHatWalkGrass;
    public AudioClip redHatJump;
    public AudioClip redHatDeath;
    public AudioClip redHatEnhanceStart;

    void Awake()
    {
        _mainSource = GetComponent<AudioSource>();
        // 保证主源无 PlayOnAwake，Loop 由 Clip 决定
        _mainSource.playOnAwake = false;
        _mainSource.loop        = false;
    }

    public void PlaySound(AudioEvent evt)
    {
        if (!photonView.IsMine) return;
        photonView.RPC(nameof(RPC_PlaySound), RpcTarget.All, (int)evt);
    }

    [PunRPC]
    private void RPC_PlaySound(int audioEventInt)
    {
        AudioEvent evt = (AudioEvent)audioEventInt;

        // 哪些事件要走“可淡出源”?
        bool needsFade = (evt == AudioEvent.WolfMagicPainting)||(evt == AudioEvent.RedHatBasketInhaling)||(evt == AudioEvent.RedHatBasketMoving);

        if (needsFade)
        {
            PlayWithTempSource(evt);
        }
        else
        {
            // 走原有的 PlayOneShot
            PlayOneShot(evt);
        }
    }
    // 普通一次性音效
    private void PlayOneShot(AudioEvent evt)
    {
        switch (evt)
        {
            case AudioEvent.WolfRunGrass:
                if (wolfRunGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        wolfRunGrass[Random.Range(0, wolfRunGrass.Length)]
                    );
                break;

            case AudioEvent.WolfWalkGrass:
                if (wolfWalkGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        wolfWalkGrass[Random.Range(0, wolfWalkGrass.Length)]
                    );
                break;

            case AudioEvent.WolfGrabPen:
                _mainSource.PlayOneShot(wolfGrabPen);
                break;

            case AudioEvent.WolfPutdownPen:
                _mainSource.PlayOneShot(wolfPutdownPen);
                // 在放下笔时，也触发对 Painting 的淡出
                StopTempSource(AudioEvent.WolfMagicPainting);
                break;
            case AudioEvent.WolfMagicClose:
                StopTempSource(AudioEvent.WolfMagicPainting);
                break;

            case AudioEvent.WolfJump:
                _mainSource.PlayOneShot(wolfJump);
                break;
            case AudioEvent.WolfDeath:
                _mainSource.PlayOneShot(wolfDeath);
                break;

            case AudioEvent.WolfEnhanceStart:
                _mainSource.PlayOneShot(wolfEnhanceStart);
                break;

             case AudioEvent.RedHatBasketInhaling:
                _mainSource.PlayOneShot(redHatBasketInhaling);
                break;
            case AudioEvent.RedHatBasketMoving:
                _mainSource.PlayOneShot(redHatBasketMoving);
                break;
            case AudioEvent.RedHatBasketTransform:
                {
                    _mainSource.PlayOneShot(redHatBasketTransform);
                    StopTempSource(AudioEvent.RedHatBasketInhaling);
                }

                break;
            case AudioEvent.RedHatGrabBasket:
                _mainSource.PlayOneShot(redHatGrabBasket);
                break;
            case AudioEvent.RedHatPutdownBasket:
                {
                    _mainSource.PlayOneShot(redHatPutdownBasket);
                    StopTempSource(AudioEvent.RedHatBasketMoving);
                }
                break;
            case AudioEvent.RedHatRunGrass:
                if (redHatRunGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        redHatRunGrass[Random.Range(0, redHatRunGrass.Length)]
                    );
                break;
            case AudioEvent.RedHatWalkGrass:
                if (redHatWalkGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        redHatWalkGrass[Random.Range(0, redHatWalkGrass.Length)]
                    );
                break;
            case AudioEvent.RedHatJump:
                _mainSource.PlayOneShot(redHatJump);
                break;
            case AudioEvent.RedHatDeath:
                _mainSource.PlayOneShot(redHatDeath);
                break;
            case AudioEvent.RedHatEnhanceStart:
                _mainSource.PlayOneShot(redHatEnhanceStart);
                break;

            // … 其余普通事件同理 …

            default:
                Debug.LogWarning("Unmapped AudioEvent: " + evt);
                break;
        }
    }

    // 使用临时 AudioSource 播放，可后续单独淡出
    // … inside CharacterAudio …

private void PlayWithTempSource(AudioEvent evt)
{
    // if there was an old temp source, kill it
    if (_tempSources.TryGetValue(evt, out var oldSrc))
    {
        oldSrc.Stop();
        Destroy(oldSrc);
        _tempSources.Remove(evt);
    }

    // create a new one
    var src = gameObject.AddComponent<AudioSource>();
    src.playOnAwake = false;
    src.loop        = false;
    src.volume      = _mainSource.volume;
    src.spatialBlend = _mainSource.spatialBlend;
    src.outputAudioMixerGroup = _mainSource.outputAudioMixerGroup;

    // map each evt → its clip
    AudioClip clip = evt switch
    {
        AudioEvent.WolfMagicPainting     => wolfMagicPainting,
        AudioEvent.RedHatBasketInhaling   => redHatBasketInhaling,   // ← add this
        AudioEvent.RedHatBasketMoving     => redHatBasketMoving,     // ← and this
        _ => null
    };

    if (clip != null)
    {
        src.clip = clip;
        src.Play();
        _tempSources[evt] = src;
    }
    else
    {
        // if we didn't actually assign a clip, clean up the extra AudioSource right away
        Destroy(src);
        Debug.LogWarning($"[CharacterAudio] no temp‐clip mapped for {evt}");
    }
}


    // 淡出并清理指定事件的临时源
    private void StopTempSource(AudioEvent evt)
    {
        if (_tempSources.TryGetValue(evt, out var src))
        {
            // DOTween 淡出到 0
            src.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                src.Stop();
                Destroy(src);
            });
            _tempSources.Remove(evt);
        }
    }

   /* public void PlaySound(AudioEvent evt) {
        switch(evt) {
            case AudioEvent.WolfRunGrass:
                if (wolfRunGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        wolfRunGrass[Random.Range(0, wolfRunGrass.Length)]
                    );
                break;

            case AudioEvent.WolfWalkGrass:
                if (wolfWalkGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        wolfWalkGrass[Random.Range(0, wolfWalkGrass.Length)]
                    );
                break;
            case AudioEvent.WolfGrabPen:
                _mainSource.PlayOneShot(wolfGrabPen);
                break;

            case AudioEvent.WolfPutdownPen:
                _mainSource.PlayOneShot(wolfPutdownPen);
                break;

            case AudioEvent.WolfMagicPainting:
                _mainSource.PlayOneShot(wolfMagicPainting);
                break;

            case AudioEvent.WolfGrabPenWalking:
                _mainSource.PlayOneShot(wolfGrabPenWalking);
                break;

            case AudioEvent.WolfJump:
                _mainSource.PlayOneShot(wolfJump);
                break;

            case AudioEvent.WolfDeath:
                _mainSource.PlayOneShot(wolfDeath);
                break;

            case AudioEvent.WolfEnhanceStart:
                _mainSource.PlayOneShot(wolfEnhanceStart);
                break;

             case AudioEvent.RedHatBasketInhaling:
                _mainSource.PlayOneShot(redHatBasketInhaling);
                break;
            case AudioEvent.RedHatBasketMoving:
                _mainSource.PlayOneShot(redHatBasketMoving);
                break;
            case AudioEvent.RedHatBasketTransform:
                _mainSource.PlayOneShot(redHatBasketTransform);
                break;
            case AudioEvent.RedHatGrabBasket:
                _mainSource.PlayOneShot(redHatGrabBasket);
                break;
            case AudioEvent.RedHatPutdownBasket:
                _mainSource.PlayOneShot(redHatPutdownBasket);
                break;
            case AudioEvent.RedHatRunGrass:
                if (redHatRunGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        redHatRunGrass[Random.Range(0, redHatRunGrass.Length)]
                    );
                break;
            case AudioEvent.RedHatWalkGrass:
                if (redHatWalkGrass.Length > 0)
                    _mainSource.PlayOneShot(
                        redHatWalkGrass[Random.Range(0, redHatWalkGrass.Length)]
                    );
                break;
            case AudioEvent.RedHatJump:
                _mainSource.PlayOneShot(redHatJump);
                break;
            case AudioEvent.RedHatDeath:
                _mainSource.PlayOneShot(redHatDeath);
                break;
            case AudioEvent.RedHatEnhanceStart:
                _mainSource.PlayOneShot(redHatEnhanceStart);
                break;

            default:
                Debug.LogWarning("Unmapped AudioEvent: " + evt);
                break;
        }
    }*/
}