using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartFireworkCore : BaseEffectCore
{
    [Header("烟花设置")]
    public ParticleSystem fireworkEffect;
    public GameObject heartPrefab;
    public float heartDropHeight = 2f;

    protected override bool NeedPhysics() => false;
        protected override void ApplyEffect(Boss_Controller bossr) { }
    protected override void OnSpawnComplete()
    {
        fireworkEffect.Play();
        DropHeart();
    }

    private void DropHeart()
    {
        GameObject heart = Instantiate(heartPrefab, 
            transform.position + Vector3.up * heartDropHeight, 
            Quaternion.identity
        );
        heart.GetComponent<HeartCore>().Initialize(skillScale);
    }

    protected override void ApplyEffect(Player_Controller player) { }
}
