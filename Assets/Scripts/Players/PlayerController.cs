using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public enum ColorType { None, Red = 1, Blue }

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float _walkSpeed = 10;
    [SerializeField] private float _rotateSpeed = 2;
    [SerializeField] private float _jumpForce = 20;

    private Rigidbody _rigidbody;
    private MeshRenderer _meshRenderer;

    private float _pitchAngle;
    private float _verticalVelocity;
    private bool _isGrounded = true;

    private GameObject _mainCam;
    public GameObject MainCam
    {
        get => _mainCam;
    }
    private NetworkVariable<ColorType> _playerColor = new NetworkVariable<ColorType>();
    public NetworkVariable<ColorType> PlayerColor
    {
        get => _playerColor;
        set => _playerColor.Value = value.Value;
    }
    private CubeController _cubeInHand;
    public CubeController CubeInHand
    {
        get => _cubeInHand;
        set => _cubeInHand = value;
    }

    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _playerColor.OnValueChanged += (ColorType before, ColorType after) =>
        {
            _meshRenderer = GetComponent<MeshRenderer>();

            if (after == ColorType.Red)
            {
                _meshRenderer.material.color = new Color(1, 0, 0);
                gameObject.layer = LayerMask.NameToLayer("Red");
            }
            else if (after == ColorType.Blue)
            {
                _meshRenderer.material.color = new Color(0, 0, 1);
                gameObject.layer = LayerMask.NameToLayer("Blue");
            }

            DebugManager.Instance.AddDebugText("Color Changed from " + before + " to " + after);

            if (IsOwner)
            {
                DebugManager.Instance.AddDebugText("Text Updated As Owner");
                NetworkUI.Instance.UpdateYourColorText(after);
            }
        };

        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count == 1) _playerColor.Value = ColorType.Red;
            else _playerColor.Value = ColorType.Blue;
        }

        if (IsOwner)
        {
            _mainCam = new GameObject("Main Camera");
            _mainCam.transform.parent = transform;
            _mainCam.AddComponent<Camera>();
            _mainCam.AddComponent<AudioListener>();
            _mainCam.tag = "MainCamera";

            Cursor.lockState = CursorLockMode.Locked;

            foreach(var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                MeshRenderer temp_meshRenderer = player.GetComponent<MeshRenderer>();

                if (player.PlayerColor.Value == ColorType.Red) temp_meshRenderer.material.color = new Color(1, 0, 0);
                else if (player.PlayerColor.Value == ColorType.Blue) temp_meshRenderer.material.color = new Color(0, 0, 1);
            }

            NetworkUI.Instance.UpdateYourColorText(_playerColor.Value);
        }
    }

    private void Start()
    {
        if (!IsOwner) return;
    }

    void Update()
    {
        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        _pitchAngle = Mathf.Clamp(_pitchAngle + mouseY * _rotateSpeed, -90, 90);

        Vector3 moveDir = (v * transform.forward + h * transform.right).normalized * _walkSpeed;
        _rigidbody.velocity = new Vector3(moveDir.x, _rigidbody.velocity.y, moveDir.z);

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            _rigidbody.velocity = new Vector3(moveDir.x, _jumpForce, moveDir.z);
            _isGrounded = false;
        }

        // ROTATE
        if (!Input.GetKey(KeyCode.Tab))
        {
            transform.Rotate(new Vector3(0, mouseX * _rotateSpeed, 0));
            Vector3 cameraRot = MainCam.transform.rotation.eulerAngles;
            MainCam.transform.rotation = Quaternion.Euler(_pitchAngle, cameraRot.y, cameraRot.z);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_cubeInHand != null)
            {
                MultiplayerManager.Instance.RemoveCubeHolderServerRpc(GetComponent<NetworkObject>(), _cubeInHand.gameObject.GetComponent<NetworkObject>());
                return;
            }

            RaycastHit[] hits = Physics.RaycastAll(transform.position, MainCam.transform.forward, 10);

            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.TryGetComponent<CubeController>(out CubeController cubeController))
                    {
                        if (cubeController.HoldingPlayer == null && _cubeInHand == null)
                        {
                            if (_playerColor.Value == cubeController.CubeColor.Value)
                            {
                                MultiplayerManager.Instance.UpdateCubeHolderServerRpc(GetComponent<NetworkObject>(), hit.collider.gameObject.GetComponent<NetworkObject>());
                            }
                        }
                    }
                }
            }
        }

        if (MultiplayerManager.Instance != null)
        {
            if (MultiplayerManager.Instance.currentRule == Rule.Red && PlayerColor.Value != ColorType.Red)
            {
                NetworkUI.Instance.ShowWarning();
            }
            else if (MultiplayerManager.Instance.currentRule == Rule.Blue && PlayerColor.Value != ColorType.Blue)
            {
                NetworkUI.Instance.ShowWarning();
            }
            else
            {
                NetworkUI.Instance.HideWarning();
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.Locked;
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            DebugManager.Instance.AddDebugText("Your Color Is " + _playerColor.Value);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ChangePlayerColorServerRpc();
        }

        if (_cubeInHand != null)
        {
            _cubeInHand.UpdateTargetPositionServerRpc(MainCam.transform.position + MainCam.transform.forward * 3);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangePlayerColorServerRpc()
    {
        if (_playerColor.Value == ColorType.Red) _playerColor.Value = ColorType.Blue;
        else _playerColor.Value = ColorType.Red;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }
}
