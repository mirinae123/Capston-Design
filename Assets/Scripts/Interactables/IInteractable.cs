using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어와 상호작용할 수 있는 물체를 나타내는 인터페이스
/// </summary>
public interface IInteractable
{
    bool StartInteraction(PlayerController player);
    bool StopInteraction(PlayerController player);
}
