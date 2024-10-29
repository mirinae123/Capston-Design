using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShootableCube : NetworkBehaviour, IShootable
{
    [SerializeField] private ColorType _cubeColor;
    private MeshRenderer _meshRenderer;

    public override void OnNetworkSpawn()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.material.color = (_cubeColor == ColorType.Red) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);
    }
    public bool OnShot(BulletController bullet)
    {
        if (_cubeColor == bullet.BulletColor)
        {
            gameObject.GetComponent<NetworkObject>().Despawn(true);
            return true;
        }
        else
        {
            return false;
        }
    }
}
