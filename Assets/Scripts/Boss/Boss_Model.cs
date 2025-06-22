using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using UnityEngine;

public class Boss_Model : ModelBase {
    // 通过回调将 AnimationEvent 转发给 Boss 控制器
    private Action redHatBasketInhalingCallback;
    private Action redHatBasketMovingCallback;
    private Action redHatBasketTransformCallback;
    private Action redHatGrabBasketCallback;
    private Action redHatPutdownBasketCallback;
    private Action redHatRunGrassCallback;
    private Action redHatWalkGrassCallback;
    private Action redHatJumpCallback;
    private Action redHatDeathCallback;
    private Action redHatEnhanceStartCallback;

    public void InitAudio(Boss_Controller owner) {
    }

    // AnimationEvent 映射方法（事件名应在动画里使用这些）
    public void OnRedHatBasketInhalingEvent() => redHatBasketInhalingCallback?.Invoke();
    public void OnRedHatBasketMovingEvent() => redHatBasketMovingCallback?.Invoke();
    public void OnRedHatBasketTransformEvent() => redHatBasketTransformCallback?.Invoke();
    public void OnRedHatGrabBasketEvent() => redHatGrabBasketCallback?.Invoke();
    public void OnRedHatPutdownBasketEvent() => redHatPutdownBasketCallback?.Invoke();
    public void OnRedHatRunGrassEvent() => redHatRunGrassCallback?.Invoke();
    public void OnRedHatWalkGrassEvent() => redHatWalkGrassCallback?.Invoke();
    public void OnRedHatJumpEvent() => redHatJumpCallback?.Invoke();
    public void OnRedHatDeathEvent() => redHatDeathCallback?.Invoke();
    public void OnRedHatEnhanceStartEvent() => redHatEnhanceStartCallback?.Invoke();
}