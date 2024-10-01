using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Runtime.CompilerServices;

/// <summary>
/// 문을 나타내는 클래스.
/// </summary>
public class DoorController : NetworkBehaviour, IActivatable
{
    /// <summary>
    /// 현재 열려 있는지 여부.
    /// </summary>
    [SerializeField] private bool _isOpen;

    /// <summary>
    /// 열린 문의 지속 시간.
    /// 0으로 설정하면 따로 닫기 전까지 계속 열려 있음.
    /// </summary>
    [SerializeField] private float _openDuration;

    [SerializeField] private Animator _animator;

    /// <summary>
    /// 문 열림 시간을 관리하기 위한 보조 변수.
    /// </summary>
    private float _timeTillClose;

    public void Update()
    {
        if (!IsServer)
        {
            return;
        }

        // 문이 열려 있고, 닫힐 때까지 시간이 남았으면
        if (_isOpen && _timeTillClose > 0)
        {
            _timeTillClose -= Time.deltaTime;

            if (_timeTillClose <= 0)
            {
                Deactivate(null);
            }
        }
    }

    public bool Activate(PlayerController player)
    {
        OpenDoorServerRpc();
        return true;
    }

    public bool Deactivate(PlayerController player)
    {
        CloseDoorServerRpc();
        return true;
    }

    /// <summary>
    /// 서버 단에서 문을 연다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void OpenDoorServerRpc()
    {
        // 문 여닫기 애니메이션이 진행 중인지 확인
        bool isPlaying = _animator.GetCurrentAnimatorStateInfo(0).length > _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        if (!_isOpen && !isPlaying)
        {
            if (_openDuration > 0)
            {
                _timeTillClose = _openDuration;
            }

            _animator.Play("Open");
            OpenDoorClientRpc();
        }
    }

    /// <summary>
    /// 클라이언트 단에서 문을 연다.
    /// 관련 변수를 각 클라이언트마다 갱신해 준다.
    /// </summary>
    [ClientRpc]
    private void OpenDoorClientRpc()
    {
        _isOpen = true;
    }

    /// <summary>
    /// 서버 단에서 문을 닫는다.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void CloseDoorServerRpc()
    {
        // 문 여닫기 애니메이션이 진행 중인지 확인
        bool isPlaying = _animator.GetCurrentAnimatorStateInfo(0).length > _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        if (_isOpen && !isPlaying)
        {
            _animator.Play("Close");
            CloseDoorClientRpc();
        }
    }

    /// <summary>
    /// 클라이언트 단에서 문을 닫는다.
    /// 관련 변수를 각 클라이언트마다 갱신해 준다.
    /// </summary>
    [ClientRpc]
    private void CloseDoorClientRpc()
    {
        _isOpen = false;
    }
}
