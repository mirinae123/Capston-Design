using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 플레이어 조작과 정보에 대한 클래스.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    // 이동 속력, 최대 이동 속력, 회전 속력, 점프력
    [SerializeField] private float _walkSpeed = 10;
    [SerializeField] private float _rotateSpeed = 2;
    [SerializeField] private float _jumpSpeed = 10;

    [SerializeField] private SkinnedMeshRenderer _meshRenderer;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;

    // 플레이어 조작에 쓰이는 보조 변수
    private Vector3 _pastPosition;
    private float _pitchAngle;
    private bool _isGrounded = true;

    // 테스트용 화면 고정 변수
    private bool _isFixed = false;

    /// <summary>
    /// 플레이어의 현재 색깔
    /// </summary>
    public NetworkVariable<ColorType> PlayerColor
    {
        get => _playerColor;
        set => _playerColor.Value = value.Value;
    }
    private NetworkVariable<ColorType> _playerColor = new NetworkVariable<ColorType>();

    /// <summary>
    /// 플레이어가 조작하는 카메라
    /// </summary>
    public GameObject MainCamera
    {
        get => _mainCamera;
    }
    private GameObject _mainCamera;

    /// <summary>
    /// 현재 상호작용 중인 물체
    /// </summary>
    public IInteractable InteractableInHand
    {
        get => _interactableInHand;
        set => _interactableInHand = value;
    }
    private IInteractable _interactableInHand;

    /// <summary>
    /// 플레이어의 현재 속도
    /// </summary>
    public Vector3 Velocity
    {
        get => _velocity;
    }
    private Vector3 _velocity;

    public override void OnNetworkSpawn()
    {
        _pastPosition = transform.position;

        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();

        // 서버에서는 플레이어 생성과 함께 색깔을 부여 (테스트용)
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count == 1) _playerColor.Value = ColorType.Red;
            else _playerColor.Value = ColorType.Blue;
        }

        // 로컬 플레이어인 경우...
        if (IsOwner)
        {
            // MultiplayerManager의 LocalPlayer 변수 설정
            MultiplayerManager.Instance.LocalPlayer = this;
            MultiplayerManager.LocalPlayerSet.Invoke();

            // 메인 카메라 생성
            _mainCamera = new GameObject("Main Camera");
            _mainCamera.transform.parent = transform;
            _mainCamera.transform.position = new Vector3(0f, 0.5f, 0.3f);
            _mainCamera.AddComponent<Camera>();
            _mainCamera.AddComponent<AudioListener>();
            _mainCamera.tag = "MainCamera";

            Cursor.lockState = CursorLockMode.Locked;
        }

        // 플레이어의 색깔이 변하면 함수 호출하도록 지정
        _playerColor.OnValueChanged += (ColorType before, ColorType after) =>
        {
            OnPlayerColorChanged(before, after);
        };

        // 플레이어 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _playerColor.OnValueChanged.Invoke(_playerColor.Value, _playerColor.Value);
            });
        }
        else
        {
            _playerColor.OnValueChanged.Invoke(_playerColor.Value, _playerColor.Value);
        }
    }

    private void FixedUpdate()
    {
        _velocity = (transform.position - _pastPosition) / Time.fixedDeltaTime;
        _pastPosition = transform.position;
    }

    private void Update()
    {
        // 로컬 플레이어가 아닌 경우 스킵
        if (!IsOwner)
        {
            return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        _pitchAngle = Mathf.Clamp(_pitchAngle + mouseY * _rotateSpeed, -90, 90);

        // 이동
        Vector3 moveDirection = (v * transform.forward + h * transform.right).normalized * _walkSpeed;
        _rigidbody.velocity = new Vector3(moveDirection.x, _rigidbody.velocity.y, moveDirection.z);

        // 접지 여부 확인
        RaycastHit[] hitss = Physics.RaycastAll(transform.position, Vector3.down, _capsuleCollider.height / 2 + 0.1f);

        if (hitss.Length > 0)
        {
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }

        // 점프
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            Vector3 newVelocity = _rigidbody.velocity;
            newVelocity.y = _jumpSpeed;

            _rigidbody.velocity = newVelocity;
        }

        // 화면 회전
        if (!_isFixed)  // 테스트용 화면 고정
        {
            transform.Rotate(new Vector3(0, mouseX * _rotateSpeed, 0));
            Vector3 cameraRotation = _mainCamera.transform.rotation.eulerAngles;
            _mainCamera.transform.rotation = Quaternion.Euler(_pitchAngle, cameraRotation.y, cameraRotation.z);
        }

        // 상호작용 키
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 이미 상호작용 중인 물체가 있다면, 상호작용 중단
            if (_interactableInHand != null)
            {
                _interactableInHand.StopInteraction(this);
            }
            // 상호작용 중인 물체가 없다면 Raycast로 탐색
            else
            {
                RaycastHit[] hits = Physics.RaycastAll(transform.position, MainCamera.transform.forward, 10);

                if (hits.Length > 0)
                {
                    foreach (RaycastHit hit in hits)
                    {
                        // hit한 물체 중 상호작용 가능한 물체가 있다면...
                        if (hit.collider.gameObject.TryGetComponent<IInteractable>(out IInteractable interactable))
                        {
                            // 상호작용 시도 후 성공 시 break
                            if (interactable.StartInteraction(this))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        // 테스트용: Tab 키를 누르면 마우스 커서가 화면을 벗어날 수 있다
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.Locked;
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 테스트용: T를 누르면 현재 색깔 표시
        if (Input.GetKeyDown(KeyCode.T))
        {
            DebugManager.Instance.AddDebugText("Your Color Is " + _playerColor.Value);
        }

        // 테스트용: Y를 누르면 플레이어 색깔 변경
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ColorType newColor = (_playerColor.Value == ColorType.Red) ? ColorType.Blue : ColorType.Red;

            ChangePlayerColorServerRpc(newColor);
        }

        // 테스트용: P를 누르면 화면 회전 고정
        if (Input.GetKeyDown(KeyCode.P))
        {
            _isFixed = !_isFixed;
        }
    }

    /// <summary>
    /// 서버 단에서 플레이어 색깔을 변경한다.
    /// </summary>
    /// <param name="newColor">변경할 색깔</param>
    [ServerRpc(RequireOwnership = false)]
    public void ChangePlayerColorServerRpc(ColorType newColor)
    {
        _playerColor.Value = newColor;
    }

    /// <summary>
    /// 플레이어의 색깔을 갱신한다
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnPlayerColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1, 0, 0) : new Color(0, 0, 1);
        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        int excludedLayer = (after == ColorType.Red) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

        /* 색깔이 다른 플레이어는 투명도 추가
        if (after != MultiplayerManager.Instance.LocalPlayer.PlayerColor.Value)
        {
            newColor.a = 0.7f;
        }
        */

        _meshRenderer.material.color = newColor;
        gameObject.layer = newLayer;

        // 다른 색깔 물체와는 물리 상호작용하지 않도록 지정
        _capsuleCollider.excludeLayers = excludedLayer;

        // 로컬 플레이어인 경우 화면 표시 갱신
        if (IsOwner)
        {
            NetworkUI.Instance.UpdateYourColorText(after);
        }
    }
}
