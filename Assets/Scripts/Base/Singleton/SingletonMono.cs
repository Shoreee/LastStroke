using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    public static T Instance;

    private void Awake()
    {
        if(Instance==null)
        {
            Instance = (T)this;
        }
    }
}
