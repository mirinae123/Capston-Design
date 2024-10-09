using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 벽을 나타내는 클래스.
/// </summary>
public class ColoredWall : NetworkBehaviour, IActivatable
{
    /// <summary>
    /// Inspector 상에서 초기 색깔을 설정하는 데 쓰이는 변수.
    /// </summary>
    [SerializeField] private ColorType _initColor;

    /// <summary>
    /// 벽의 현재 색깔.
    /// </summary>
    public NetworkVariable<ColorType> WallColor
    {
        get => _wallColor;
        set => _wallColor.Value = value.Value;
    }
    private NetworkVariable<ColorType> _wallColor = new NetworkVariable<ColorType>();

    private BoxCollider _boxCollider;
    private MeshRenderer _meshRenderer;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _wallColor.Value = _initColor;
        }

        _boxCollider = GetComponent<BoxCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();

        // 벽의 색깔이 변하면 함수 호출하도록 지정
        _wallColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnWallColorChanged(before, after);
        };

        // 벽 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _wallColor.OnValueChanged.Invoke(_wallColor.Value, _wallColor.Value);
            });
        }
        else
        {
            _wallColor.OnValueChanged.Invoke(_wallColor.Value, _wallColor.Value);
        }
    }

    public bool Activate(PlayerController player)
    {
        UpdateWallServerRpc(false);

        return true;
    }

    public bool Deactivate(PlayerController player)
    {
        return false;
    }

    /// <summary>
    /// 벽의 색깔을 갱신한다.
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnWallColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1, 0, 0, 0.5f) : new Color(0, 0, 1, 0.5f);

        // 파란색 벽은 파란색 물체와 물리 상호작용하지 않는다.
        // 따라서 벽의 레이어는 "Red"로, 제외 레이어는 "Blue"로 둔다.
        // 빨간색 벽도 이러한 방식으로 설정한다.
        // int newLayer = (after == ColorType.Blue) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        int excludedLayer = (after == ColorType.Blue) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

        _meshRenderer.material.color = newColor;
        // gameObject.layer = newLayer;

        // 다른 색깔 물체와는 물리 상호작용하지 않도록 지정
        _boxCollider.excludeLayers = excludedLayer;
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateWallServerRpc(bool active)
    {
        UpdateWallClientRpc(active);
    }

    [ClientRpc]
    private void UpdateWallClientRpc(bool active)
    {
        _boxCollider.enabled = active;
        _meshRenderer.enabled = active;
    }
}
