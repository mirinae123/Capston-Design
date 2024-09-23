using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 스위치를 나타내는 클래스.
/// </summary>
public class SwitchController : NetworkBehaviour, IInteractable
{
    /// <summary>
    /// 두 명이 함께 있어야 열리는지 여부.
    /// </summary>
    [SerializeField] private bool _isRequireBoth;

    /// <summary>
    /// 어떤 색깔 플레이어가 스위치를 눌러야 하는지.
    /// _isRequireBoth가 체크돼 있으면 이 변수는 무시한다.
    /// </summary>
    [SerializeField] private ColorType _switchColor;

    /// <summary>
    /// 두 명이 함께 있어야 할 때, 플레이어 인식 범위
    /// </summary>
    [SerializeField] private float _switchRange;

    /// <summary>
    /// 스위치가 활성화할 게임 오브젝트
    /// </summary>
    [SerializeField] private GameObject _connectedGameObject;

    /// <summary>
    /// 스위치가 활성화할 게임 오브젝트의 IInteractable 클래스
    /// </summary>
    private IInteractable _connectedInteractable;

    public bool Activate(PlayerController player)
    {
        return false;
    }

    public bool Deactivate(PlayerController player)
    {
        return false;
    }

    public bool StartInteraction(PlayerController player)
    {
        // 두 명이 함께 있어야 하는 경우
        if (_isRequireBoth)
        {
            // 스위치 주변에 있는 플레이어 수를 확인
            if (GetNumberOfPlayersNearby() >= 2)
            {
                _connectedInteractable.Activate(player);
                return true;
            }
            else
            {
                return false;
            }
        }
        // 혼자서 누를 수 있는 경우
        else
        {
            // 스위치 색깔이 None이면 아무나 누를 수 있다
            if (_switchColor == ColorType.None)
            {
                _connectedInteractable.Activate(player);
                return true;
            }
            // 그렇지 않으면 플레이어 색깔을 확인한다
            else if (_switchColor == player.PlayerColor.Value)
            {
                _connectedInteractable.Activate(player);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool StopInteraction(PlayerController player)
    {
        return false;
    }

    public override void OnNetworkSpawn()
    {
        _connectedInteractable = _connectedGameObject.GetComponent<IInteractable>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (_switchColor == ColorType.None)
        {
            meshRenderer.material.color = new Color(1, 0, 1);
        }
        else if (_switchColor == ColorType.Red)
        {
            meshRenderer.material.color = new Color(1, 0, 0);
        }
        else
        {
            meshRenderer.material.color = new Color(0, 0, 1);
        }
    }

    /// <summary>
    /// 스위치 주변(_switchRange)에 있는 플레이어의 수를 반환한다.
    /// </summary>
    private int GetNumberOfPlayersNearby()
    {
        PlayerController[] playerControllers = GameObject.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        int numberOfPlayersInRange = 0;

        foreach (PlayerController playerController in playerControllers)
        {
            float distance = Vector3.Distance(transform.position, playerController.transform.position);

            if (distance <= _switchRange)
            {
                numberOfPlayersInRange++;
            }
        }

        return numberOfPlayersInRange;
    }
}
