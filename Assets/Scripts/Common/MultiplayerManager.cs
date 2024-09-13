using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum Rule { None, Blue, Yellow, Red }

public class MultiplayerManager : NetworkBehaviour
{
    public static MultiplayerManager Instance;

    public Rule currentRule = Rule.Yellow;
    float timeTillUpdate = 5f;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        NetworkUI.Instance.UpdateLightCircles();
    }

    void Update()
    {
        if (!IsServer) return;

        timeTillUpdate -= Time.deltaTime;

        if (timeTillUpdate < 0)
        {
            if (currentRule == Rule.Yellow)
            {
                float rand = Random.value;

                if (rand > 0.5f) UpdateRuleClientRpc(Rule.Red);
                else UpdateRuleClientRpc(Rule.Blue);

                timeTillUpdate = 4f;
            }
            else
            {
                UpdateRuleClientRpc(Rule.Yellow);
                timeTillUpdate = 1.5f;
            }
        }
    }

    [ClientRpc]
    public void UpdateRuleClientRpc(Rule newRule)
    {
        currentRule = newRule;
        NetworkUI.Instance.UpdateLightCircles();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateCubeHolderServerRpc(NetworkObjectReference player, NetworkObjectReference cube)
    {
        UpdateCubeHolderClientRpc(player, cube);
    }

    [ClientRpc]
    public void UpdateCubeHolderClientRpc(NetworkObjectReference player, NetworkObjectReference cube)
    {
        if (player.TryGet(out NetworkObject playerObj) && cube.TryGet(out NetworkObject cubeObj))
        {
            playerObj.GetComponent<PlayerController>().HoldCube = cubeObj.GetComponent<CubeController>();
            cubeObj.GetComponent<CubeController>().PlayerController = playerObj.GetComponent <PlayerController>();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveCubeHolderServerRpc(NetworkObjectReference player, NetworkObjectReference cube)
    {
        RemoveCubeHolderClientRpc(player, cube);
    }

    [ClientRpc]
    public void RemoveCubeHolderClientRpc(NetworkObjectReference player, NetworkObjectReference cube)
    {
        if (player.TryGet(out NetworkObject playerObj) && cube.TryGet(out NetworkObject cubeObj))
        {
            playerObj.GetComponent<PlayerController>().HoldCube = null;
            cubeObj.GetComponent<CubeController>().PlayerController = null;
        }
    }
}
