using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    //类对象池
    private Dictionary<Type, object> classObjectPools = new Dictionary<Type, object>();
    
    public ClassObjectPool<T> GetOrCreateClassObjectPool<T>(int maxCount = 0) where T :class,IReusable,new()
    {
        Type type = typeof(T);
        if (!classObjectPools.TryGetValue(type, out object pool) || pool == null)
        {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxCount);
            classObjectPools.Add(type,newPool);
            return newPool;
        }

        return pool as ClassObjectPool<T>;
    }
}
