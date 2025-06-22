using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public abstract class CharacterBase<T>: MonoBehaviourPun, IStateMachineOwner, ISkillOwner, IHurt  where T : ModelBase
{
    //[SerializeField] protected ModelBase model;
    [SerializeField] protected T player_Model;
    public T Player_Model { get => player_Model; }
    public T Boss_Model { get => player_Model; }
    public Transform ModelTransform => player_Model.transform;
    [SerializeField] protected CharacterController characterController;
    public CharacterController CharacterController { get => characterController; }
    protected StateMachine stateMachine;

    //protected CharacterAudio audioModule;
    public List<string> enemeyTagList;

    public virtual void Init()
    {
        player_Model.Init(this, enemeyTagList);
        stateMachine = new StateMachine();
        stateMachine.Init(this);
        //audioModule = GetComponent<CharacterAudio>();
        //if (audioModule == null) {
        //    audioModule = gameObject.AddComponent<CharacterAudio>();
        //}
    }

    #region 技能相关
    public SkillConfig CurrentSkillConfig { get; private set; }
    protected int currentHitIndex = 0;
    // 可以切换技能，主要用于判定前摇和后摇
    public bool CanSwitchSkill { get; private set; }
    public void StartAttack(SkillConfig skillConfig)
    {
        CanSwitchSkill = false; // 防止玩家立刻播放下一个技能
        CurrentSkillConfig = skillConfig;
        currentHitIndex = 0;
        // 播放技能动画
        PlayAnimation(CurrentSkillConfig.AnimationName);
        // 技能释放音效
        SpawnSkillObject(skillConfig.ReleaseData.SpawnObj);
        // 技能释放物体
        PlayAudio(CurrentSkillConfig.ReleaseData.AudioClip);
    }
    public void StartSkillHit(int weaponIndex)
    {
        // 技能释放音效
        SpawnSkillObject(CurrentSkillConfig.AttackData[currentHitIndex].SpawnObj);
        // 技能释放物体
        PlayAudio(CurrentSkillConfig.AttackData[currentHitIndex].AudioClip);
    }

    public void StopSkillHit(int weaponIndex)
    {
        currentHitIndex += 1;
    }

    public void SkillCanSwitch()
    {
        CanSwitchSkill = true;
    }

    private void SpawnSkillObject(Skill_SpawnObj spawnObj)
    {
        if (spawnObj != null && spawnObj.Prefab != null)
        {
            StartCoroutine(DoSpawnObject(spawnObj));
        }
    }

    protected IEnumerator DoSpawnObject(Skill_SpawnObj spawnObj)
    {
        // 延迟时间
        yield return new WaitForSeconds(spawnObj.Time);
        GameObject skillObj = GameObject.Instantiate(spawnObj.Prefab, null);
        // 一般特效的生成位置是相对于主角的
        skillObj.transform.position = player_Model.transform.position + player_Model.transform.TransformDirection(spawnObj.Position);
        skillObj.transform.localScale = spawnObj.Scale;
        skillObj.transform.eulerAngles = player_Model.transform.eulerAngles + spawnObj.Rotation;
        PlayAudio(spawnObj.AudioClip);
    }

    public virtual void OnHit(IHurt target, Vector3 hitPostion)
    {
        // 拿到这一段攻击的数据
        Skill_AttackData attackData = CurrentSkillConfig.AttackData[currentHitIndex];
        // 生成基于命中配置的效果
        StartCoroutine(DoSkillHitEF(attackData.SkillHitEFConfig, hitPostion));
        StartFreezeFrame(attackData.FreezeFrameTime);
        StartFreezeTime(attackData.FreezeGameTime);
        // 传递伤害数据
        target.Hurt(attackData.HitData, this);
    }

    protected void StartFreezeFrame(float time)
    {
        if (time > 0) StartCoroutine(DoFreezeFrame(time));
    }

    protected IEnumerator DoFreezeFrame(float time)
    {
        player_Model.Animator.speed = 0;
        yield return new WaitForSeconds(time);
        player_Model.Animator.speed = 1;
    }

    protected void StartFreezeTime(float time)
    {
        if (time > 0) StartCoroutine(DoFreezeTime(time));
    }

    protected IEnumerator DoFreezeTime(float time)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(time);
        Time.timeScale = 1;
    }


    protected IEnumerator DoSkillHitEF(SkillHitEFConfig hitEFConfig, Vector3 spawnPoint)
    {
        if (hitEFConfig == null) yield break;
        // 延迟时间
        PlayAudio(hitEFConfig.AudioClip);
        if (hitEFConfig.SpawnObject != null && hitEFConfig.SpawnObject.Prefab != null)
        {
            yield return new WaitForSeconds(hitEFConfig.SpawnObject.Time);
            GameObject temp = Instantiate(hitEFConfig.SpawnObject.Prefab);
            temp.transform.position = spawnPoint + hitEFConfig.SpawnObject.Position;
            temp.transform.LookAt(Camera.main.transform);
            temp.transform.eulerAngles += hitEFConfig.SpawnObject.Rotation;
            temp.transform.localScale += hitEFConfig.SpawnObject.Scale;
            PlayAudio(hitEFConfig.SpawnObject.AudioClip);
        }
    }

    public void OnSkillOver()
    {
        CanSwitchSkill = true;
    }


    #endregion

    public void PlayAnimation(string animationName, float fixedTransitionDuration = 0.25f)
    {
        player_Model.Animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration);
    }

    public void PlayAudio(AudioClip clip) {
        //if (clip != null)
        //    audioModule.PlaySound(AudioEvent.FootStep); // 这里保留原PlayAudio逻辑，如有特殊Clip可直接调用audioModule._audioSource.PlayOneShot
    }

    public virtual void Hurt(Skill_HitData hitData, ISkillOwner hurtSource)
    {
    }
}
