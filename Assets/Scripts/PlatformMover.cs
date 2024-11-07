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

        Vector3 pos = transform.position;
        pos.y = (Mathf.Sin(acc_time) + 1) * 5;
        GetComponent<Rigidbody>().MovePosition(pos);
    }

    [ServerRpc(RequireOwnership =false)]
    private void ParentServerRpc(NetworkObjectReference networkRef)
    {
        if (networkRef.TryGet(out NetworkObject networkObject))
        {
            GetComponent<NetworkObject>().ChangeOwnership(networkObject.OwnerClientId);
            networkObject.TrySetParent(GetComponent<NetworkObject>());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnparentServerRpc(NetworkObjectReference networkRef)
    {
        if (networkRef.TryGet(out NetworkObject networkObject))
        {
            networkObject.TryRemoveParent();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            if (networkObject.transform.parent != transform)
            {
                ParentServerRpc(networkObject);
                DebugManager.Instance.AddDebugText("Parented");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {       
            if (networkObject.transform.parent == transform)
            {
                UnparentServerRpc(networkObject);
                DebugManager.Instance.AddDebugText("Unparented");
            }
        }
    }
}
