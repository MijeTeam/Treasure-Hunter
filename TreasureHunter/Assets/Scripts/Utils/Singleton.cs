using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> where T : class, new()
{
    private static T ins = null;
    static readonly object objLock = new object();

    public static T Ins
    {
        get
        {
            if(ins == null)
            {
                lock(objLock)
                {
                    ins = new T();
                }
            }
            return ins;
        }
    }
}

public class Singleton2<T> : MonoBehaviour where T : MonoBehaviour
{
    static T ins = null;

    public static T Ins
    {
        get
        {
            if(ins == null)
            {
                GameObject go = new GameObject();
                go.name = "== Singleton class";

                DontDestroyOnLoad(go);
                ins = go.AddComponent(typeof(T)) as T;
            }
            return ins;
        }
    }
}
