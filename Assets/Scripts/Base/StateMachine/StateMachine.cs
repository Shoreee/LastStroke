using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public interface IStateMachineOwner {}
public class StateMachine:IPunObservable
{

    private IStateMachineOwner owner;

    private Dictionary<Type,StateBase> stateDic = new Dictionary<Type,StateBase>();

    public Type CurrentStateType{ get => currentState.GetType();}

    public bool HasState{ get =>currentState!=null;}
    public StateBase CurrentState => currentState; // 添加此行以允许外部访问 currentState
    private StateBase currentState;
    public void Init(IStateMachineOwner owner)
    {
        this.owner = owner;
    }

    public bool ChangeState<T>(bool reCurrstate = false)where T:StateBase,new()
    {
        //状态一致
        if(currentState!=null &&CurrentStateType == typeof(T) && !reCurrstate) return false;
        //退出
        if(currentState !=null)
        {
            currentState.Exit();
            MonoManager.Instance.RemoveUpdateListener(currentState.Update);
            MonoManager.Instance.RemoveLateUpdateListener(currentState.LateUpdate);
            MonoManager.Instance.RemoveFixedUpdateListener(currentState.FixedUpdate);
        }

        //进入新状态
        currentState = GetState<T>();
        currentState.Enter();
        MonoManager.Instance.AddUpdateListener(currentState.Update);
        MonoManager.Instance.AddLateUpdateListener(currentState.LateUpdate);
        MonoManager.Instance.AddFixedUpdateListener(currentState.FixedUpdate);
        return false;
    }

    private StateBase GetState<T>() where T : StateBase,new()
    {
        Type type = typeof(T);
        //如果字典中不存在
        if(!stateDic.TryGetValue(typeof(T), out StateBase state))
        {    
            state = new T();
            state.Init(owner);
            stateDic.Add(type, state);
        }
        return state;
    }

    private void Stop()
    {
            currentState.Exit();
            MonoManager.Instance.RemoveUpdateListener(currentState.Update);
            MonoManager.Instance.RemoveLateUpdateListener(currentState.LateUpdate);
            MonoManager.Instance.RemoveFixedUpdateListener(currentState.FixedUpdate);
            currentState = null;

            foreach(var item in stateDic.Values)
            {
                item.UnInit();
            }
            stateDic.Clear();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
    }
}
