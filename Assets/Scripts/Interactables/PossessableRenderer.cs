using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PossessableRenderer : NetworkBehaviour
{
    private PossessableController _possessableController;
    private MeshRenderer _meshRenderer;

    public override void OnNetworkSpawn()
    {
        _possessableController.PossessableColor.OnValueChanged += (ColorType before, ColorType after) => {
            OnPossessableColorChanged(before, after);
        };

        // 큐브 최초 생성 후 초기화 작업을 수행
        // MultiplayerManager의 LocalPlayer를 참조하므로, 해당 변수가 지정될 때까지 대기
        if (MultiplayerManager.Instance.LocalPlayer == null)
        {
            MultiplayerManager.LocalPlayerSet.AddListener(() =>
            {
                _possessableController.PossessableColor.OnValueChanged.Invoke(_possessableController.PossessableColor.Value, _possessableController.PossessableColor.Value);
            });
        }
        else
        {
            _possessableController.PossessableColor.OnValueChanged.Invoke(_possessableController.PossessableColor.Value, _possessableController.PossessableColor.Value);
        }
    }

    private void Awake()
    {
        _possessableController = GetComponent<PossessableController>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnPossessableColorChanged(ColorType before, ColorType after)
    {
        Color newColor = (after == ColorType.Red) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.3f, 1f);

        _meshRenderer.material.color = newColor;
    }
}
