using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRenderer : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;

    private PlayerController _playerController;

    public override void OnNetworkSpawn()
    {
        _playerController = GetComponent<PlayerController>();

        // 플레이어의 색깔이 변하면 함수 호출하도록 지정
        _playerController.PlayerColor.OnValueChanged += (ColorType before, ColorType after) =>
        {
            OnPlayerColorChanged(before, after);
        };

        // 플레이어 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _playerController.PlayerColor.OnValueChanged.Invoke(_playerController.PlayerColor.Value, _playerController.PlayerColor.Value);
            });
        }
        else
        {
            _playerController.PlayerColor.OnValueChanged.Invoke(_playerController.PlayerColor.Value, _playerController.PlayerColor.Value);
        }
    }

    private void OnPlayerColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1, 0, 0) : new Color(0, 0, 1);

        _skinnedMeshRenderer.material.color = newColor;
    }
}
