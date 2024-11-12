using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletRenderer : NetworkBehaviour
{
    public void UpdateMeshColor()
    {
        BulletController bulletController = GetComponent<BulletController>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material.color = (bulletController.BulletColor == ColorType.Red) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);
    }
}
