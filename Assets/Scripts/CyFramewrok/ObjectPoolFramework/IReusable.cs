using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IReusable
{
    abstract void OnSpawn();
    abstract void OnRecycle();
}
