using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 빙의 가능한 물체를 조작하는 클래스.
/// </summary>
public class PossessableController : NetworkBehaviour, IInteractable
{
    /// <summary>
    /// Inspector 상에서 초기 색깔을 설정하는 데 쓰이는 변수.
    /// </summary>
    [SerializeField] private ColorType _initColor;

    /// <summary>
    /// 물체의 현재 색깔.
    /// </summary>
    public NetworkVariable<ColorType> PossessableColor
    {
        get => _possessableColor;
        set => _possessableColor.Value = value.Value;
    }
    private NetworkVariable<ColorType> _possessableColor = new NetworkVariable<ColorType>();

    /// <summary>
    /// 빙의한 플레이어.
    /// </summary>
    public PlayerController PossessingPlayer
    {
        get => _possessingPlayer;
        set => _possessingPlayer = value;
    }
    private PlayerController _possessingPlayer;

    private Rigidbody _rigidbody;

    private Collider _collider;

    public override void OnNetworkSpawn()
    {
        _possessableColor.Value = _initColor;

        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        // 물체의 색깔이 변하면 함수 호출하도록 지정
        _possessableColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnPossessableColorChanged(before, after);
        };

        // 물체 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _possessableColor.OnValueChanged.Invoke(_possessableColor.Value, _possessableColor.Value);
            });
        }
        else
        {
            _possessableColor.OnValueChanged.Invoke(_possessableColor.Value, _possessableColor.Value);
        }
    }

    /// <summary>
    /// 물체와 상호작용을 시작한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    public bool StartInteraction(PlayerController player)
    {
        // 색깔이 일치하지 않으면 무시
        if (_possessableColor.Value != player.PlayerColor.Value)
        {
            return false;
        }

        // 서버 단에서 상호 작용 상태 갱신
        AddHoldingPlayerServerRpc(player.NetworkObject);

        return true;
    }

    /// <summary>
    /// 물체와 상호작용을 중단한다.
    /// </summary>
    public bool StopInteraction(PlayerController player)
    {
        // 서버 단에서 상호 작용 상태 갱신
        RemoveHoldingPlayerServerRpc();

        return true;
    }

    public bool IsInteractable(PlayerController player)
    {
        return player.PlayerColor.Value == _possessableColor.Value;
    }

    private void OnPossessableColorChanged(ColorType before, ColorType after)
    {
        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        // int excludedLayer = (after == ColorType.Red) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

        gameObject.layer = newLayer;

        // 다른 색깔 물체와는 물리 상호작용하지 않도록 지정
        // _capsuleCollider.excludeLayers = excludedLayer;
    }

    /// <summary>
    /// 서버 단에서 물체와 상호작용을 시작한다. Parent를 지정하고 모든 클라이언트에게 정보를 전달한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    [ServerRpc(RequireOwnership = false)]
    private void AddHoldingPlayerServerRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject playerNetworkObject))
        {
            GetComponent<NetworkObject>().TrySetParent(playerNetworkObject.gameObject.transform);

            AddHoldingPlayerClientRpc(player);
        }
    }

    /// <summary>
    /// 클라이언트 단에서 물체와 상호작용을 시작한다. 관련 상태를 갱신한다.
    /// </summary>
    /// <param name="player">상호작용할 플레이어</param>
    [ClientRpc]
    private void AddHoldingPlayerClientRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject playerNetworkObject))
        {
            _possessingPlayer = playerNetworkObject.gameObject.GetComponent<PlayerController>();

            // 플레이어와 물체의 위치를 조정한다.
            if (IsOwner)
            {
                _possessingPlayer.transform.position = transform.position;
            }

            transform.localPosition = Vector3.zero;

            _possessingPlayer.InteractableInHand = this;

            // 콜라이더의 종류에 따라 높이 계산
            if (_collider is BoxCollider)
            {
                _possessingPlayer.Height = (_collider as BoxCollider).size.y * transform.localScale.y;
            }

            if (_collider is CapsuleCollider)
            {
                _possessingPlayer.Height = (_collider as CapsuleCollider).height * transform.localScale.y;
            }

            // 플레이어의 CapusleCollider를 비활성화하고, 메쉬를 숨긴다.
            _possessingPlayer.gameObject.GetComponent<Collider>().enabled = false;
            _possessingPlayer.gameObject.GetComponent<PlayerRenderer>().HidePlayerMesh();

            // 물체는 별도로 Network Transform을 사용하지 않게 한다.
            GetComponent<ClientNetworkTransform>().enabled = false;

            // Rigidbody는 플레이어의 것을 쓰도록 한다.
            Destroy(_rigidbody);
        }
    }

    /// <summary>
    /// 서버 단에서 물체와 상호작용을 중단한다. Parent를 제거하고 클라이언트에 정보를 전달한다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RemoveHoldingPlayerServerRpc()
    {
        GetComponent<NetworkObject>().TrySetParent(null as Transform);

        RemoveHoldingPlayerClientRpc();
    }

    /// <summary>
    /// 클라이언트 단에서 물체와 상호작용을 중단한다. 관련 상태를 갱신한다.
    /// </summary>
    [ClientRpc]
    private void RemoveHoldingPlayerClientRpc()
    {
        // 플레이어의 CapusleCollider를 활성화하고, 메쉬를 표시한다.
        _possessingPlayer.gameObject.GetComponent<Collider>().enabled = true;
        _possessingPlayer.gameObject.GetComponent<PlayerRenderer>().ShowPlayerMesh();

        _possessingPlayer.InteractableInHand = null;
        _possessingPlayer.Height = _possessingPlayer.GetComponent<CapsuleCollider>().height * _possessingPlayer.gameObject.transform.localScale.y;

        _possessingPlayer = null;

        // 별도로 Network Transform을 사용하게 한다.
        GetComponent<ClientNetworkTransform>().enabled = true;

        // 별도의 Rigidbody를 사용하게 한다.
        _rigidbody = gameObject.AddComponent<Rigidbody>();
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rigidbody.angularDrag = 5f;
    }
}
