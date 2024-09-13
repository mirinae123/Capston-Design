using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum CubeColors { Blue, Red };

public class CubeController : NetworkBehaviour
{
    public CubeColors CubeColor;
    public PlayerController PlayerController;

    private Rigidbody rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (CubeColor == CubeColors.Blue)
        {
            meshRenderer.material.color = new Color(0, 0, 1);
        }
        else if (CubeColor == CubeColors.Red)
        {
            meshRenderer.material.color = new Color(1, 0, 0);
        }

        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerController != null)
        {
            rigidbody.useGravity = false;
            Vector3 target = PlayerController.transform.position + PlayerController.MainCamHolder.transform.forward * 3;
            float magnitude = (target - transform.position).magnitude;

            rigidbody.velocity = (target - transform.position) * magnitude * magnitude;
        }
        else
        {
            rigidbody.useGravity = true;
        }
    }
}
