using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using UnityEngine;


public class Player_Model : ModelBase {
    // 通过回调将 AnimationEvent 转发给 CharacterBase
    private Action footStepCallback;
    private Action wolfRunGrassCallback;
    private Action wolfWalkGrassCallback;
    private Action wolfGrabPenCallback;
    private Action wolfPutdownPenCallback;
    private Action wolfMagicPaintingCallback;
    private Action wolfGrabPenWalkingCallback;
    private Action wolfJumpCallback;
    private Action wolfDeathCallback;
    private Action wolfEnhanceStartCallback;

    public void InitAudio(Player_Controller owner) {

    }

    // AnimationEvent 调用
    public void OnWolfRunGrassEvent() => wolfRunGrassCallback?.Invoke();
    public void OnWolfWalkGrassEvent() => wolfWalkGrassCallback?.Invoke();
    public void OnWolfGrabPenEvent() => wolfGrabPenCallback?.Invoke();
    public void OnWolfPutdownPenEvent() => wolfPutdownPenCallback?.Invoke();
    public void OnWolfMagicPaintingEvent() => wolfMagicPaintingCallback?.Invoke();
    public void OnWolfGrabPenWalkingEvent() => wolfGrabPenWalkingCallback?.Invoke();
    public void OnWolfJumpEvent() => wolfJumpCallback?.Invoke();
    public void OnWolfDeathEvent() => wolfDeathCallback?.Invoke();
    public void OnWolfEnhanceStartEvent() => wolfEnhanceStartCallback?.Invoke();
}
