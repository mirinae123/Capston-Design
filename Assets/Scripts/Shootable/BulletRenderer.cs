using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletRenderer : NetworkBehaviour
{
    private BulletController _bulletController;
    private MeshRenderer _meshRenderer;

    override public void OnNetworkSpawn()
    {
        _bulletController = GetComponent<BulletController>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void UpdateMeshColor()
    {
        _meshRenderer.material.color = (_bulletController.BulletColor == ColorType.Red) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);
    }
}
