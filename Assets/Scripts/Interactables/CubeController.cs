using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CubeController : NetworkBehaviour
{
    [SerializeField] private ColorType _initColor;

    private NetworkVariable<ColorType> _cubeColor = new NetworkVariable<ColorType>();
    public NetworkVariable<ColorType> CubeColor
    {
        get => _cubeColor;
    }
    private PlayerController _holdingPlayer;
    public PlayerController HoldingPlayer
    {
        get => _holdingPlayer;
        set => _holdingPlayer = value;
    }
    private Vector3 _targetPosition;

    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;
    private MeshRenderer _meshRenderer;

    // Start is called before the first frame update
    void Start()
    {
        CubeColor.Value = _initColor;

        _meshRenderer = GetComponent<MeshRenderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();

        CubeColor.OnValueChanged += (ColorType before, ColorType after) => {
            if (after == ColorType.Blue)
            {
                _meshRenderer.material.color = new Color(0, 0, 1);
                _boxCollider.excludeLayers = 1 << LayerMask.NameToLayer("Red");
            }
            else if (after == ColorType.Red)
            {
                _meshRenderer.material.color = new Color(1, 0, 0);
                _boxCollider.excludeLayers = 1 << LayerMask.NameToLayer("Blue");
            }
        };

        CubeColor.OnValueChanged.Invoke(CubeColor.Value, CubeColor.Value);
    }

    // Update is called once per frame
    void Update()
    {
        if (_holdingPlayer != null)
        {
            _rigidbody.useGravity = false;
            _rigidbody.velocity = (_targetPosition - transform.position) * 10;
        }
        else
        {
            _rigidbody.useGravity = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateTargetPositionServerRpc(Vector3 newPosition)
    {
        UpdateTargetPositionClientRpc(newPosition);
    }

    [ClientRpc]
    public void UpdateTargetPositionClientRpc(Vector3 newPosition)
    {
        _targetPosition = newPosition;
    }
}
