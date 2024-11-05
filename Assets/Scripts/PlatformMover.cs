using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlatformMover : NetworkBehaviour
{
    float acc_time = 0f;

    float exit_cool = 3f;
    NetworkObject queue_obj = null;
    
    void Update()
    {
        if (queue_obj != null)
        {
            exit_cool -= Time.deltaTime;
            if (exit_cool < 0f)
            {
                OnCollisionExitServerRpc(queue_obj);
                queue_obj = null;
            }
        }

        if (!IsServer)
        {
            return;
        }

        acc_time += Time.deltaTime;

        if (acc_time > 2 * Mathf.PI)
        {
            acc_time = 0;
        }

        Vector3 pos = transform.position;
        pos.y = (Mathf.Sin(acc_time) + 1) * 5;
        GetComponent<Rigidbody>().MovePosition(pos);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            queue_obj = null;

            if (networkObject.transform.parent != transform)
            {
                OnCollisionEnterServerRpc(networkObject);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            queue_obj = networkObject;
            exit_cool = 3f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnCollisionEnterServerRpc(NetworkObjectReference networkRef)
    {
        if (networkRef.TryGet(out NetworkObject networkObject))
        {
            networkObject.TrySetParent(this.NetworkObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void OnCollisionExitServerRpc(NetworkObjectReference networkRef)
    {
        if (networkRef.TryGet(out NetworkObject networkObject))
        {
            networkObject.TryRemoveParent();
        }
    }
}
