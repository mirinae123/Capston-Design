using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 색깔 enum
/// </summary>
public enum ColorType { None, Blue, Red }

/// <summary>
/// 멀티플레이어 구현을 지원하는 싱글톤 클래스
/// </summary>
public class MultiplayerManager : NetworkBehaviour
{
    public static MultiplayerManager Instance;

    /// <summary>
    /// LocalPlayer가 지정되어 더 이상 NULL이 아닌 경우 Invoke한다.
    /// OnNetworkSpawn(), Start() 등에서 LocalPlayer를 참조하는 경우 NULL 값을 참조하지 않기 위함이다.
    /// </summary>
    private static UnityEvent _localPlayerSet = new UnityEvent();
    public static UnityEvent LocalPlayerSet
    {
        get => _localPlayerSet;
    }

    private PlayerController _localPlayer;
    public PlayerController LocalPlayer
    {
        get => _localPlayer;
        set => _localPlayer = value;
    }

    public void Awake()
    {
        Instance = this;
    }
}
