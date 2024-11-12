using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HitZone : NetworkBehaviour
{
    private MeshRenderer _meshRenderer;
    private bool _isPlayerInside = false;

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.material.color = new Color(1f, .5f, .5f, .5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<PlayerController>(out PlayerController player) &&
            player.IsLocalPlayer) {
            _isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<PlayerController>(out PlayerController player) &&
            player.IsLocalPlayer) {
            _isPlayerInside = false;
        }
    }

    [ClientRpc]
    public void EnableHitZoneClientRpc()
    {
        _meshRenderer.enabled = true;
    }

    [ClientRpc]
    public void DoHitDamageClientRpc()
    {
        if (_isPlayerInside)
        {
            PlayerController localPlayer = MultiplayerManager.Instance.LocalPlayer;

            if (localPlayer.PlayerColor.Value == ColorType.Red)
            {
                Vector3 targetPosition = GameObject.FindGameObjectWithTag("RedPlayerSpawn").transform.position;
                localPlayer.gameObject.transform.position = targetPosition;
            }
            else
            {
                Vector3 targetPosition = GameObject.FindGameObjectWithTag("BluePlayerSpawn").transform.position;
                localPlayer.gameObject.transform.position = targetPosition;
            }
         }
    }

    [ClientRpc]
    public void DisableHitZoneClientRpc()
    {
        _meshRenderer.enabled = false;
    }
}
