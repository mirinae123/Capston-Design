using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRenderer : NetworkBehaviour
{
    [SerializeField] private GameObject _redPlayerPrefab;
    [SerializeField] private GameObject _bluePlayerPrefab;

    [SerializeField] private RuntimeAnimatorController _redAnimatorController;
    [SerializeField] private RuntimeAnimatorController _blueAnimatorController;

    private PlayerController _playerController;

    private GameObject _playerMeshObject;
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private Animator _animator;

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

    private void Update()
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetFloat("speed", _playerController.Velocity.magnitude);
    }

    private void OnPlayerColorChanged(ColorType before, ColorType after)
    {
        if (_playerMeshObject != null)
        {
            Destroy(_playerMeshObject);
        }

        if (after == ColorType.Red)
        {
            _playerMeshObject = Instantiate(_redPlayerPrefab, transform);
            _playerMeshObject.transform.localPosition = new Vector3(-1.2f, -1f, 0f);
            _animator = _playerMeshObject.AddComponent<Animator>();
            _animator.runtimeAnimatorController = _redAnimatorController;
        }
        if (after == ColorType.Blue)
        {
            _playerMeshObject = Instantiate(_bluePlayerPrefab, transform);
            _playerMeshObject.transform.localPosition = new Vector3(-1.2f, -1f, 0f);
            _animator = _playerMeshObject.AddComponent<Animator>();
            _animator.runtimeAnimatorController = _blueAnimatorController;
        }

        _skinnedMeshRenderer = _playerMeshObject.GetComponentInChildren<SkinnedMeshRenderer>();

        Color newColor = (after == ColorType.Red) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);

        _skinnedMeshRenderer.material.color = newColor;
    }
}
