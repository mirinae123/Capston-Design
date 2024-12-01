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
        _animator = GetComponent<Animator>();

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

    public void ShowPlayerMesh()
    {
        if (_playerMeshObject.activeSelf)
        {
            return;
        }

        _playerMeshObject?.SetActive(true);
    }

    public void HidePlayerMesh()
    {
        if (!_playerMeshObject.activeSelf)
        {
            return;
        }

        _playerMeshObject?.SetActive(false);
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
            _playerMeshObject = Instantiate(_redPlayerPrefab);
            _playerMeshObject.transform.position = transform.position;
            _playerMeshObject.transform.rotation = transform.rotation;
            _playerMeshObject.AddComponent<NetworkSyncInterpolator>().Target = gameObject;
            _animator = _playerMeshObject.AddComponent<Animator>();
            _animator.runtimeAnimatorController = _redAnimatorController;
        }
        if (after == ColorType.Blue)
        {
            _playerMeshObject = Instantiate(_bluePlayerPrefab);
            _playerMeshObject.transform.position = transform.position;
            _playerMeshObject.transform.rotation = transform.rotation;
            _playerMeshObject.AddComponent<NetworkSyncInterpolator>().Target = gameObject;
            _animator = _playerMeshObject.AddComponent<Animator>();
            _animator.runtimeAnimatorController = _blueAnimatorController;
        }

        if (IsOwner)
        {
            _playerController.MainCamera = new GameObject();
            _playerController.MainCamera.transform.parent = _playerMeshObject.transform;
            _playerController.MainCamera.transform.position = new Vector3(0f, 0.6f, 0.3f);
            _playerController.MainCamera.AddComponent<Camera>();
            _playerController.MainCamera.AddComponent<AudioListener>();
            _playerController.MainCamera.tag = "MainCamera";
        }

        _skinnedMeshRenderer = _playerMeshObject.GetComponentInChildren<SkinnedMeshRenderer>();

        Color newColor = (after == ColorType.Red) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);

        _skinnedMeshRenderer.material.color = newColor;
    }
}
