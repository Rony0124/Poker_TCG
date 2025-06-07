using System;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    public static T Instance {
        get {
            if (instance == null) {
                instance = FindObjectOfType(typeof(T)) as T;

                if (instance == null) {
                    var singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<T>();

                    singletonObject.name = typeof(T).Name;
                }
            }
                
            return instance;
        }
    }
        
    protected virtual void Awake ()
    {
        InitializeSingleton();		
    }

    protected virtual void InitializeSingleton()
    {
        if(instance == null)
            instance = this as T;
    }
}
