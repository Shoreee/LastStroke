using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public abstract class ModelBase : MonoBehaviourPun
{
    [SerializeField] protected Animator animator;
    public Animator Animator { get => animator; }
    protected ISkillOwner skillOwner;
    [SerializeField] Weapon_Controller[] weapons;
    public void Init(ISkillOwner skillOwner, List<string> enemeyTagList)
    {
        this.skillOwner = skillOwner;
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].Init(enemeyTagList, skillOwner.OnHit);
        }
    }

    #region 根运动
    protected Action<Vector3, Quaternion> rootMotionAction;

    public void SetRootMotionAction(Action<Vector3, Quaternion> rootMotionAction)
    {
        this.rootMotionAction = rootMotionAction;
    }

    public void ClearRootMotionAction()
    {
        rootMotionAction = null;
    }

    private void OnAnimatorMove()
    {
        rootMotionAction?.Invoke(animator.deltaPosition, animator.deltaRotation);
    }
    #endregion

    #region 动画事件
    protected void StartSkillHit(int weaponIndex)
    {
        skillOwner.StartSkillHit(weaponIndex);
        weapons[weaponIndex].StartSkillHit();
    }

    protected void StopSkillHit(int weaponIndex)
    {
        skillOwner.StopSkillHit(weaponIndex);
        weapons[weaponIndex].StopSkillHit();
    }
    protected void SkillCanSwitch()
    {
        skillOwner.SkillCanSwitch();
    }
    #endregion
}
