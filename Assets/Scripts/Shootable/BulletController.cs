using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BulletController : NetworkBehaviour
{
    public ColorType BulletColor
    {
        get { return _bulletColor; }
    }
    private ColorType _bulletColor;

    public void Initialize(ColorType bulletColor, Vector3 direction)
    {
        GetComponent<Rigidbody>().velocity = direction * 32f;

        _bulletColor = bulletColor;
        GetComponent<BulletRenderer>().UpdateMeshColor();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("HitZone"))
        {
            return;
        }

        if (other.gameObject.TryGetComponent<IShootable>(out IShootable shootable))
        {
            shootable.OnShot(this);
        }

        Destroy(gameObject);
    }
}
