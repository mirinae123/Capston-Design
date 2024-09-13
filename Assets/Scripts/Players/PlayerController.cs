using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PlayerColor { None, Red = 1, Blue }

public class PlayerController : NetworkBehaviour
{
    private CharacterController characterController;
    private Rigidbody rigidbody;
    public GameObject MainCam { get; private set; }
    public GameObject MainCamHolder { get; private set; }

    [SerializeField] private float walkSpeed;
    [SerializeField] private float rotateSpeed;
    [SerializeField] private float jumpForce;

    private float pitchAngle;
    private float verticalVelocity;
    private bool isGrounded = true;

    public NetworkVariable<PlayerColor> playerColor = new NetworkVariable<PlayerColor>();
    public CubeController HoldCube;

    public override void OnNetworkSpawn()
    {
        characterController = GetComponent<CharacterController>();
        rigidbody = GetComponent<Rigidbody>();

        playerColor.OnValueChanged += (PlayerColor before, PlayerColor after) =>
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

            if (after == PlayerColor.Red)
            {
                meshRenderer.material.color = new Color(1, 0, 0);
                gameObject.layer = LayerMask.NameToLayer("Red");
            }
            else if (after == PlayerColor.Blue)
            {
                meshRenderer.material.color = new Color(0, 0, 1);
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
            if (NetworkManager.Singleton.ConnectedClients.Count == 1) playerColor.Value = PlayerColor.Red;
            else playerColor.Value = PlayerColor.Blue;
        }

        MainCamHolder = new GameObject("Main Camera Holder");
        MainCamHolder.transform.parent = transform;

        if (IsOwner)
        {
            MainCam = new GameObject("Main Camera");
            MainCam.transform.parent = MainCamHolder.transform;
            MainCam.AddComponent<Camera>();
            MainCam.AddComponent<AudioListener>();
            MainCam.tag = "MainCamera";

            Cursor.lockState = CursorLockMode.Locked;

            foreach(var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                MeshRenderer meshRenderer = player.GetComponent<MeshRenderer>();

                if (player.playerColor.Value == PlayerColor.Red) meshRenderer.material.color = new Color(1, 0, 0);
                else if (player.playerColor.Value == PlayerColor.Blue) meshRenderer.material.color = new Color(0, 0, 1);
            }

            NetworkUI.Instance.UpdateYourColorText(playerColor.Value);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        pitchAngle = Mathf.Clamp(pitchAngle + mouseY * rotateSpeed, -90, 90);

        /*
        // HORIZONTAL MOVE
        Vector3 moveDir = (v * transform.forward + h * transform.right).normalized * walkSpeed;
        characterController.Move(moveDir * Time.deltaTime);

        // JUMP
        if (isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpForce;
                isGrounded = false;
            }
            else
            {
                verticalVelocity = 0;
            }
        }
        else
        {
            verticalVelocity += Physics.gravity.y * 10f * Time.deltaTime;
        }

        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        */

        Vector3 moveDir = (v * transform.forward + h * transform.right).normalized * walkSpeed;
        rigidbody.velocity = new Vector3(moveDir.x, rigidbody.velocity.y, moveDir.z);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rigidbody.velocity = new Vector3(moveDir.x, jumpForce, moveDir.z);
            isGrounded = false;
        }

        // ROTATE
        if (!Input.GetKey(KeyCode.Tab))
        {
            transform.Rotate(new Vector3(0, mouseX * rotateSpeed, 0));
            Vector3 cameraRot = MainCamHolder.transform.rotation.eulerAngles;
            MainCamHolder.transform.rotation = Quaternion.Euler(pitchAngle, cameraRot.y, cameraRot.z);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (HoldCube != null)
            {
                MultiplayerManager.Instance.RemoveCubeHolderServerRpc(GetComponent<NetworkObject>(), HoldCube.gameObject.GetComponent<NetworkObject>());
                return;
            }

            RaycastHit[] hits = Physics.RaycastAll(transform.position, MainCam.transform.forward, 10);

            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.TryGetComponent<CubeController>(out CubeController cubeController))
                    {
                        if (cubeController.PlayerController == null && HoldCube == null)
                        {
                            bool sameColor = (playerColor.Value == PlayerColor.Red && cubeController.CubeColor == CubeColors.Red) ||
                                        (playerColor.Value == PlayerColor.Blue && cubeController.CubeColor == CubeColors.Blue);

                            if (sameColor) MultiplayerManager.Instance.UpdateCubeHolderServerRpc(GetComponent<NetworkObject>(), hit.collider.gameObject.GetComponent<NetworkObject>());
                        }
                    }
                }
            }
        }

        if (MultiplayerManager.Instance != null)
        {
            if (MultiplayerManager.Instance.currentRule == Rule.Red && playerColor.Value != PlayerColor.Red)
            {
                NetworkUI.Instance.ShowWarning();
            }
            else if (MultiplayerManager.Instance.currentRule == Rule.Blue && playerColor.Value != PlayerColor.Blue)
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
            DebugManager.Instance.AddDebugText("Your Color Is " + playerColor.Value);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ChangePlayerColorServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangePlayerColorServerRpc()
    {
        if (playerColor.Value == PlayerColor.Red) playerColor.Value = PlayerColor.Blue;
        else playerColor.Value = PlayerColor.Red;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
