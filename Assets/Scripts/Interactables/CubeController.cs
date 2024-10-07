using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상자를 나타내는 클래스.
/// </summary>
public class CubeController : NetworkBehaviour, IInteractable
{
    /// <summary>
    /// Inspector 상에서 초기 색깔을 설정하는 데 쓰이는 변수.
    /// </summary>
    [SerializeField] private ColorType _initColor;

    /// <summary>
    /// 상자의 현재 색깔.
    /// </summary>
    public NetworkVariable<ColorType> CubeColor
    {
        get => _cubeColor;
        set => _cubeColor.Value = value.Value;
    }
    private NetworkVariable<ColorType> _cubeColor = new NetworkVariable<ColorType>();

    /// <summary>
    /// 상자를 들고 있는 플레이어. 아무도 들고 있지 않으면 NULL이다.
    /// </summary>
    public PlayerController HoldingPlayer
    {
        get => _holdingPlayer;
        set => _holdingPlayer = value;
    }
    private PlayerController _holdingPlayer;

    /// <summary>
    /// 색깔을 변경한 후, 새로운 색깔이 지속될 시간.
    /// </summary>
    public float ColorChangeDuration
    {
        get => _colorChangeDuration.Value;
        set => _colorChangeDuration.Value = value;
    }
    private NetworkVariable<float> _colorChangeDuration = new NetworkVariable<float>();

    /// <summary>
    /// 색깔이 원래대로 돌아올 때까지 남은 시간.
    /// </summary>
    public float ColorChangeTimeLeft
    {
        get => _colorChangeTimeLeft.Value;
        set => _colorChangeTimeLeft.Value = value;
    }
    private NetworkVariable<float> _colorChangeTimeLeft = new NetworkVariable<float>();

    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _cubeColor.Value = _initColor;
        }

        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();

        // 큐브의 색깔이 변하면 함수 호출하도록 지정
        _cubeColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnCubeColorChanged(before, after);
        };

        // 큐브 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _cubeColor.OnValueChanged.Invoke(_cubeColor.Value, _cubeColor.Value);
            });
        }
        else
        {
            _cubeColor.OnValueChanged.Invoke(_cubeColor.Value, _cubeColor.Value);
        }
    }

    /// <summary>
    /// 상자와 상호작용을 시작한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    public bool StartInteraction(PlayerController player)
    {
        // 색깔이 일치하지 않으면 무시
        if (_cubeColor.Value != player.PlayerColor.Value)
        {
            return false;
        }

        // 서버 단에서 상호 작용 상태 갱신
        AddHoldingPlayerServerRpc(player.NetworkObject);

        return true;
    }

    /// <summary>
    /// 상자와 상호작용을 중단한다.
    /// </summary>
    public bool StopInteraction(PlayerController player)
    {
        // 서버 단에서 상호 작용 상태 갱신
        RemoveHoldingPlayerServerRpc();

        return true;
    }

    public bool IsInteractable(PlayerController player)
    {
        return player.PlayerColor.Value == _cubeColor.Value;
    }
    
    /// <summary>
    /// 상자 색깔을 원래(_initColor)의 반대로 변경한다.
    /// </summary>
    public void ChangeCubeColor()
    {
        _cubeColor.Value = (_initColor == ColorType.Red) ? ColorType.Blue : ColorType.Red;
    }

    /// <summary>
    /// 상자 색깔을 원래 색깔(_initColor)로 되돌린다.
    /// </summary>
    public void RestoreCubeColor()
    {
        _cubeColor.Value = _initColor;
    }

    private void Update()
    {
        if (IsOwner)
        {
            // 상자를 들고 있는 플레이어(Owner)가 상자 이동을 담당한다
            if (_holdingPlayer != null)
            {
                Vector3 target = _holdingPlayer.transform.position + _holdingPlayer.transform.forward * 1.5f;
                _rigidbody.velocity = (target - transform.position) * 16f;
            }
        }

        if (IsServer)
        {
            // 서버에서 색깔 변환 지속 시간을 담당한다
            if (_colorChangeTimeLeft.Value > 0f)
            {
                _colorChangeTimeLeft.Value -= Time.deltaTime;

                if (_colorChangeTimeLeft.Value <= 0f)
                {
                    RestoreCubeColor();
                }
            }
        }
    }

    /// <summary>
    /// 상자의 색깔을 갱신한다.
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnCubeColorChanged(ColorType before, ColorType after)
    {
        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        // int excludedLayer = (after == ColorType.Red) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

        gameObject.layer = newLayer;

        // 다른 색깔 물체와는 물리 상호작용하지 않도록 지정
        // _boxCollider.excludeLayers = excludedLayer;

        // 누군가 들고 있는 상태에서 색깔이 변한 경우, 강제로 놓는다
        if (IsServer && _holdingPlayer != null)
        {
            RemoveHoldingPlayerServerRpc();
        }
    }

    /// <summary>
    /// 서버 단에서 상자와 상호작용을 시작한다. Ownership을 넘기고 모든 클라이언트에 정보를 전달한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    [ServerRpc(RequireOwnership = false)]
    private void AddHoldingPlayerServerRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject playerNetworkObject))
        {
            _rigidbody.useGravity = false;

            // 플레이어가 상자를 조작할 수 있도록 Ownership 변경
            GetComponent<NetworkObject>().ChangeOwnership(playerNetworkObject.OwnerClientId);

            AddHoldingPlayerClientRpc(player);
        }
    }

    /// <summary>
    /// 클라이언트 단에서 상자와 상호작용을 시작한다. 관련 변수를 갱신한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    [ClientRpc]
    private void AddHoldingPlayerClientRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject playerNetworkObject))
        {
            _holdingPlayer = playerNetworkObject.gameObject.GetComponent<PlayerController>();
            _holdingPlayer.InteractableInHand = this;
        }
    }

    /// <summary>
    /// 서버 단에서 상자와 상호작용을 중단한다. Ownership을 넘기고 모든 클라이언트에 정보를 전달한다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RemoveHoldingPlayerServerRpc()
    {
        GetComponent<NetworkObject>().RemoveOwnership();
        _rigidbody.useGravity = true;

        RemoveHoldingPlayerClientRpc();
    }

    /// <summary>
    /// 클라이언트 단에서 상자와 상호작용을 중단한다. 관련 변수를 갱신한다.
    /// </summary>
    [ClientRpc]
    private void RemoveHoldingPlayerClientRpc()
    {
        _holdingPlayer.InteractableInHand = null;
        _holdingPlayer = null;
    }
}
