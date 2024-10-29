using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IShootable
{
    public bool OnShot(BulletController bullet);
}
