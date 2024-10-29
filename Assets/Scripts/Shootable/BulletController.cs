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

    private Rigidbody _rigidbody;

    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(ColorType bulletColor, Vector3 direction)
    {
        _rigidbody.velocity = direction * 32f;

        ChangeColorClientRpc(bulletColor);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<IShootable>(out IShootable shootable))
        {
            shootable.OnShot(this);
        }

        Destroy(gameObject);
    }

    [ClientRpc]
    public void ChangeColorClientRpc(ColorType bulletColor)
    {
        _bulletColor = bulletColor;
        GetComponent<BulletRenderer>().UpdateMeshColor();
    }
}
