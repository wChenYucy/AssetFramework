using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassObjectPool<T> where T : class, IReusable, new()
{
    protected Stack<T> pool = new Stack<T>();

    protected int maxCount = 0;
    
    protected int noRecycleCount = 0;

    public ClassObjectPool(int maxCount)
    {
        this.maxCount = maxCount;
        for (int i = 0; i < maxCount; i++)
        {
            pool.Push(new T());
        }
    }

    public T Spawn(bool createIfPoolEmpty)
    {
        T item = null;
        if (pool.Count > 0)
        {
            item = pool.Pop();
            if (item == null)
            {
                if (createIfPoolEmpty)
                {
                    item = new T();
                }
            }
        }
        else
        {
            if (createIfPoolEmpty)
            {
                item = new T();
            }
        }

        if (item != null)
        {
            item.OnSpawn();
            noRecycleCount++;
        }
            
        return item;
    }

    public bool Recycle(T item)
    {
        if (item == null)
            return false;
        item.OnRecycle();
        if (pool.Count >= maxCount && maxCount > 0)
        {
            item = null;
            return false;
        }

        noRecycleCount--;
        pool.Push(item);
        return true;
    }
}
