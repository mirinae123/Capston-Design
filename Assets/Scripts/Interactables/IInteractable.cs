using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어와 상호작용할 수 있는 물체를 나타내는 인터페이스
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 플레이어와 상호작용을 시작한다.
    /// </summary>
    /// <param name="player">대상 플레이어</param>
    /// <returns></returns>
    public bool StartInteraction(PlayerController player);

    /// <summary>
    /// 플레이어와 상호작용을 중단한다.
    /// </summary>
    /// <param name="player">대상 플레이어</param>
    /// <returns></returns>
    public bool StopInteraction(PlayerController player);

    /// <summary>
    /// 플레이어가 물체와 상호작용할 수 있는지 반환한다.
    /// </summary>
    /// <param name="player">대상 플레이어</param>
    /// <returns>상호작용 가능 여부</returns>
    public bool IsInteractable(PlayerController player);
}
