using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프레이어가 간접적으로 활성화할 수 있는 물체(문 등)을 나타내는 인터페이스
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// 물체를 활성화한다.
    /// </summary>
    /// <param name="player">대상 플레이어</param>
    /// <returns></returns>
    public bool Activate(PlayerController player);

    /// <summary>
    /// 물체를 비활성화한다.
    /// </summary>
    /// <param name="player">대상 플레이어</param>
    /// <returns></returns>
    public bool Deactivate(PlayerController player);
}
