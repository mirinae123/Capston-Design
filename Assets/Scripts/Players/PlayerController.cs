using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 플레이어 조작과 정보에 대한 클래스.
/// </summary>
public class PlayerController : NetworkSyncObject
{
    // 이동 속력, 최대 이동 속력, 회전 속력, 점프력
    [SerializeField] private float _walkSpeed = 10;
    [SerializeField] private float _rotateSpeed = 2;
    [SerializeField] private float _jumpSpeed = 10;

    [SerializeField] private GameObject _bulletPrefab;

    private CapsuleCollider _capsuleCollider;

    // 플레이어 조작에 쓰이는 보조 변수
    private float _pitchAngle;

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
    /// 플레이어의 메인 카메라
    /// </summary>
    public GameObject MainCamera
    {
        get => _mainCamera;
        set => _mainCamera = value;
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
    /// 플레이어의 키.
    /// </summary>
    public float Height
    {
        get => _height;
        set => _height = value;
    }
    private float _height;

    /// <summary>
    /// 플레이어의 현재 속도
    /// </summary>
    public Vector3 Velocity
    {
        get => _rigidbody.velocity;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _capsuleCollider = GetComponent<CapsuleCollider>();

        _height = _capsuleCollider.height * transform.localScale.y;

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
            InitLocalPlayer();
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

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
    }

    private void Update()
    {

        // 로컬 플레이어가 아닌 경우 스킵
        if (!IsOwner)
        {
            return;
        }

        // 플레이어 회전
        // Rotate();

        // 플레이어가 보고 있는 물체 확인
        CheckInteractable();

        // 상호작용
        Interact();

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

        if (Input.GetKeyDown(KeyCode.F))
        {
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            SpawnBulletServerRpc(_playerColor.Value, _mainCamera.transform.position, _mainCamera.transform.forward);

            gameObject.layer = LayerMask.NameToLayer(_playerColor.Value.ToString());      
        }
    }

    /// <summary>
    /// 로컬 플레이어를 초기화한다.
    /// </summary>
    private void InitLocalPlayer()
    {
        // 초기 스폰 위치 설정
        GameObject[] spawnPositions;

        if (_playerColor.Value == ColorType.Red)
        {
            spawnPositions = GameObject.FindGameObjectsWithTag("RedPlayerSpawn");
        }
        else
        {
            spawnPositions = GameObject.FindGameObjectsWithTag("BluePlayerSpawn");
        }

        if (spawnPositions.Length > 0)
        {
            transform.position = spawnPositions[0].transform.position;
        }

        // MultiplayerManager의 LocalPlayer 변수 설정
        MultiplayerManager.Instance.LocalPlayer = this;
        MultiplayerManager.LocalPlayerSet.Invoke();

        // 메인 카메라 생성
        //_mainCamera = new GameObject();
        //_mainCamera.transform.parent = transform;
        //_mainCamera.transform.position = new Vector3(0f, 0.6f, 0.3f);
        //_mainCamera.AddComponent<Camera>();
        //_mainCamera.AddComponent<AudioListener>();
        //_mainCamera.tag = "MainCamera";

        Cursor.lockState = CursorLockMode.Locked;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendInputPayloadServerRpc(InputPayload inputPayload)
    {
        _inputQueue.Enqueue(inputPayload);
    }

    [ClientRpc]
    private void SendStatePayloadClientRpc(StatePayload statePayload)
    {
        _stateQueue.Enqueue(statePayload);
    }

    public override bool GetInput()
    {
        if (IsOwner)
        {
            float horizontalKey = Input.GetAxis("Horizontal");
            float verticalKey = Input.GetAxis("Vertical");

            float horizontalMouse = Input.GetAxis("Mouse X");
            float verticalMouse = -Input.GetAxis("Mouse Y");

            bool jumpPushed = Input.GetKey(KeyCode.Space);

            Vector3 moveDirection = (verticalKey * transform.forward + horizontalKey * transform.right).normalized * _walkSpeed;

            _pitchAngle = Mathf.Clamp(_pitchAngle + verticalMouse * _rotateSpeed, -90, 90);

            _processingInput.tick = NetworkSyncManager.Instance.CurrentTick;

            _processingInput.inputVector.x = moveDirection.x;
            _processingInput.inputVector.y = jumpPushed ? _jumpSpeed : 0;
            _processingInput.inputVector.z = moveDirection.z;

            _processingInput.subVector.y = horizontalMouse * _rotateSpeed;

            Vector3 cameraRotation = _mainCamera.transform.rotation.eulerAngles;
            _mainCamera.transform.rotation = Quaternion.Euler(_pitchAngle, cameraRotation.y, cameraRotation.z);

            return true;
        }
        else
        {
            return false;
        }
    }

    public override void ApplyInput(InputPayload inputPayload)
    {
        _rigidbody.velocity = new Vector3(inputPayload.inputVector.x, inputPayload.inputVector.y > 0 ? (IsGrounded() ? _jumpSpeed : _rigidbody.velocity.y) : _rigidbody.velocity.y, inputPayload.inputVector.z);
        transform.Rotate(inputPayload.subVector);
    }

    /// <summary>
    /// 플레이어를 회전한다.
    /// </summary>
    private void Rotate()
    {
        float h = Input.GetAxis("Mouse X");
        float v = -Input.GetAxis("Mouse Y");

        _pitchAngle = Mathf.Clamp(_pitchAngle + v * _rotateSpeed, -90, 90);

        // transform.Rotate(new Vector3(0, h * _rotateSpeed, 0));
        Vector3 cameraRotation = _mainCamera.transform.rotation.eulerAngles;
        _mainCamera.transform.rotation = Quaternion.Euler(_pitchAngle, cameraRotation.y, cameraRotation.z);
    }

    /// <summary>
    /// 플레이어를 점프시킨다.
    /// </summary>
    private void Jump()
    {
        
    }

    /// <summary>
    /// 접지 여부를 확인한다
    /// </summary>
    /// <returns>접지 여부</returns>
    private bool IsGrounded()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, _height / 2f + 0.2f);

        if (hits.Length > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 상호작용 가능한 물체를 탐색한다.
    /// </summary>
    private void CheckInteractable()
    {
        if (_interactableInHand == null)
        {
            // 레이캐스트 동안에는 플레이어 무시
            int currentLayer = gameObject.layer;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward * 5f, out RaycastHit hit);
            gameObject.layer = currentLayer;

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
    }

    private void Interact()
    {
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

    [ServerRpc(RequireOwnership = false)]
    public void SpawnBulletServerRpc(ColorType bulletColor, Vector3 position, Vector3 direction)
    {
        SpawnBulletClientRpc(bulletColor, position, direction);
    }

    [ClientRpc]
    public void SpawnBulletClientRpc(ColorType bulletColor, Vector3 position, Vector3 direction)
    {
        GameObject bullet = Instantiate(_bulletPrefab);

        bullet.transform.position = position + direction;
        bullet.GetComponent<BulletController>().Initialize(bulletColor, direction);
    }

    /// <summary>
    /// 플레이어의 색깔을 갱신한다
    /// </summary>
    /// <param name="before">변경 전 색깔</param>
    /// <param name="after">변경 후 색깔</param>
    private void OnPlayerColorChanged(ColorType before, ColorType after)
    {
        int newLayer = (after == ColorType.Red) ? LayerMask.NameToLayer("Red") : LayerMask.NameToLayer("Blue");
        // int excludedLayer = (after == ColorType.Red) ? LayerMask.GetMask("Blue") : LayerMask.GetMask("Red");

        gameObject.layer = newLayer;

        // 다른 색깔 물체와는 물리 상호작용하지 않도록 지정
        // _capsuleCollider.excludeLayers = excludedLayer;

        // 로컬 플레이어인 경우 화면 표시 갱신
        if (IsOwner)
        {
            NetworkUI.Instance.UpdateYourColorText(after);
        }
    }
}