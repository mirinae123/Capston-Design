using Unity.Netcode;
using UnityEngine;

public class PlatformMover : NetworkSyncObject
{
    float acc_time = 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        GameObject.Find("Elevator").GetComponent<NetworkSyncInterpolator>().Target = gameObject                             ;
    }

    void Update()
    {
        if (!IsServer)
        {
            return;
        }

        acc_time += Time.deltaTime;

        if (acc_time > 2 * Mathf.PI)
        {
            acc_time = 0;
        }
    }

    public override bool GetInput()
    {
        Debug.Log("GetInput");
        if (IsServer)
        {
            _processingInput.tick = NetworkSyncManager.Instance.CurrentTick;
            _processingInput.inputVector.y = (Mathf.Sin(acc_time) + 1) * 5;
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void ApplyInput(InputPayload inputPayload)
    {
        Vector3 pos = transform.position;
        pos.y = inputPayload.inputVector.y;
        GetComponent<Rigidbody>().MovePosition(pos);
    }
}
