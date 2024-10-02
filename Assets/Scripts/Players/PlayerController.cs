//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Security.Claims;
//using System.Text;
//using Unity.Burst.CompilerServices;
using Cinemachine;
using System.Data;
using TMPro;
using Unity.Netcode;
using UnityEditor;

//using Unity.VisualScripting;
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

    private CinemachineFreeLook _mainCamera;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;

    // 플레이어 조작에 쓰이는 보조 변수
    private float _pitchAngle;
    private float _yawAngle;
    private bool _isGrounded = true;

    // 테스트용 화면 고정 변수
    private bool _isFixed = false;

    // 플레이어가 보고 있는 물체
    private GameObject _objectOnPointer = null;
    private IInteractable _interactableOnPointer = null;

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
        get => _rigidbody.velocity;
    }

    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();

        // 플레이어의 색깔이 변하면 함수 호출하도록 지정
        _playerColor.OnValueChanged += (ColorType before, ColorType after) =>
        {
            OnPlayerColorChanged(before, after);
        };

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
            _mainCamera = GameObject.FindAnyObjectByType<CinemachineFreeLook>();
            _mainCamera.Follow = transform;
            _mainCamera.LookAt = transform;

            Cursor.lockState = CursorLockMode.Locked;
        }

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

        _yawAngle += mouseX * _rotateSpeed;

        if (_yawAngle < 0f)
        {
            _yawAngle = 360f;
        }
        else if (_yawAngle > 360f)
        {
            _yawAngle = 0f;
        }

        // 이동
        Vector3 moveDirection = _mainCamera.State.FinalOrientation * new Vector3(h, 0, v).normalized * _walkSpeed;
        _rigidbody.velocity = new Vector3(moveDirection.x, _rigidbody.velocity.y, moveDirection.z);

        if (_rigidbody.velocity.magnitude > 0f && _yawAngle != transform.rotation.eulerAngles.y)
        {
            transform.transform.rotation = Quaternion.Euler(0f, _mainCamera.State.FinalOrientation.eulerAngles.y, 0f);
        }

        // 접지 여부 확인
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, _capsuleCollider.height / 2 + 0.2f);

        if (hits.Length > 0)
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
            // _cameraController.TargetPosition = Quaternion.Euler(_pitchAngle, _yawAngle, 0) * (transform.position + transform.up - _cameraController.transform.forward * 2f);
        }

        // 플레이어가 보고 있는 물체 확인
        if (_interactableInHand == null)
        {
            Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out RaycastHit hit, 5f);

            if (hit.collider == null)
            {
                if (_objectOnPointer != null)
                {
                    _objectOnPointer.GetComponent<Outline>().enabled = false;
                    _objectOnPointer = null;
                }
            }
            else
            {
                if (_objectOnPointer != hit.collider.gameObject)
                {
                    if (_objectOnPointer != null)
                    {
                        _objectOnPointer.GetComponent<Outline>().enabled = false;
                        _objectOnPointer = null;
                    }

                    if (hit.collider.gameObject.TryGetComponent<IInteractable>(out IInteractable interactable) && interactable.IsInteractable(this))
                    {
                        _objectOnPointer = hit.collider.gameObject;
                        _interactableOnPointer = interactable;
                        _objectOnPointer.GetComponent<Outline>().enabled = true;
                    }
                }
                else if (_objectOnPointer != null && !_interactableOnPointer.IsInteractable(this))
                {
                    _objectOnPointer.GetComponent<Outline>().enabled = false;
                    _objectOnPointer = null;
                }
            }
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
            else if (_objectOnPointer != null)
            {
                _objectOnPointer.GetComponent<IInteractable>().StartInteraction(this);

                // 하이라이트 제거
                _objectOnPointer.GetComponent<Outline>().enabled = false;
                _objectOnPointer = null;
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
        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        int excludedLayer = (after == ColorType.Red) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

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
