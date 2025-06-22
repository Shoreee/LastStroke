using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Controller : MonoBehaviour
{
    [SerializeField] private new Collider collider;

    private List<string> enemeyTagList;
    private List<IHurt> enemyList = new List<IHurt>();
    private Action<IHurt, Vector3> onHitAction;
    public void Init(List<string> enemeyTagList, Action<IHurt, Vector3> onHitAction)
    {
        this.enemeyTagList = enemeyTagList;
        this.onHitAction = onHitAction;
        collider.enabled = false;
    }

    public void StartSkillHit()
    {
        collider.enabled = true;
    }

    public void StopSkillHit()
    {
        collider.enabled = false;
        enemyList.Clear();
    }

    private void OnTriggerStay(Collider other)
    {
        if (enemeyTagList == null) return;
        // 检测打击对象的标签
        if (enemeyTagList.Contains(other.tag))
        {
            IHurt enemey = other.GetComponentInParent<IHurt>();
            // 如果此次攻击，攻击过这个单位，则不产生攻击
            if (enemey != null && !enemyList.Contains(enemey))
            {
                // 通知上级处理命中
                onHitAction?.Invoke(enemey, other.ClosestPoint(transform.position));
                enemyList.Add(enemey);
            }
        }
    }
}
