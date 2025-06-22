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
    public StateBase CurrentState => currentState; // ��Ӵ����������ⲿ���� currentState
    private StateBase currentState;
    public void Init(IStateMachineOwner owner)
    {
        this.owner = owner;
    }

    public bool ChangeState<T>(bool reCurrstate = false)where T:StateBase,new()
    {
        //״̬һ��
        if(currentState!=null &&CurrentStateType == typeof(T) && !reCurrstate) return false;
        //�˳�
        if(currentState !=null)
        {
            currentState.Exit();
            MonoManager.Instance.RemoveUpdateListener(currentState.Update);
            MonoManager.Instance.RemoveLateUpdateListener(currentState.LateUpdate);
            MonoManager.Instance.RemoveFixedUpdateListener(currentState.FixedUpdate);
        }

        //������״̬
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
        //����ֵ��в�����
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
