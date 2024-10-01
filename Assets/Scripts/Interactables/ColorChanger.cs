using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 색깔 변환기를 나타내는 클래스.
/// </summary>
public class ColorChanger : NetworkBehaviour
{
    /// <summary>
    /// 바꾼 색상이 지속될 시간.
    /// </summary>
    [SerializeField] private float _colorChangeDuration;

    public override void OnNetworkSpawn()
    {
        GetComponent<MeshRenderer>().material.color = new Color(1f, 0.3f, 1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
        {
            return;
        }

        // 변환기 위에 상호작용 가능한 물체가 올라오면...
        if (collision.gameObject.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            // 상자인 경우: 일단 색깔 바꾸기
            if (interactable is CubeController)
            {
                (interactable as CubeController).ChangeCubeColor();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsServer)
        {
            return;
        }

        // 변환기 위에서 상호작용 가능한 물체가 나가면...
        if (collision.gameObject.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            // 상자인 경우: 지속 시간 적용
            if (interactable is CubeController)
            {
                CubeController cubeController = (CubeController)interactable;

                cubeController.ColorChangeDuration = _colorChangeDuration;
                cubeController.ColorChangeTimeLeft = _colorChangeDuration;
            }
        }
    }
}
