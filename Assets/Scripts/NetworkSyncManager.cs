using System;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkSyncManager : NetworkBehaviour
{
    public static NetworkSyncManager Instance;

    private float _timer = 0f;
    private bool _isRunning = false;

    public int CurrentTick
    {
        get => _currentTick;
    }
    private int _currentTick = 0;

    public bool NeedReconcile
    {
        get => _needReconcile;
        set => _needReconcile = value;
    }
    private bool _needReconcile = false;

    public int ReconcileTick
    {
        get => _reconcileTick;
        set => _reconcileTick = value;
    }
    private int _reconcileTick = 0;

    public int LastReconciledTick
    {
        get => _lastReconciledTick;
        set => _lastReconciledTick = value;
    }
    private int _lastReconciledTick = 0;

    public Action GetClientInput
    {
        get => _getClientInput;
        set => _getClientInput = value;
    }
    private Action _getClientInput;

    public Action GetClientState
    {
        get => _getClientState;
        set => _getClientState = value;
    }
    private Action _getClientState;

    public Action GetServerInput
    {
        get => _getServerInput;
        set => _getServerInput = value;
    }
    private Action _getServerInput;

    public Action GetServerState
    {
        get => _getServerState;
        set => _getServerState = value;
    }
    private Action _getServerState;

    public Action GetReconcileCondition
    {
        get => _getReconcileCondition;
        set => _getReconcileCondition = value;
    }
    private Action _getReconcileCondition;

    public Action<int> PreReconcile
    {
        get => _preReconcile;
        set => _preReconcile = value;
    }
    private Action<int> _preReconcile;

    public Action<int> GetReconcileInput
    {
        get => _getReconcileInput;
        set => _getReconcileInput = value;
    }
    private Action<int> _getReconcileInput;

    public Action GetReconcileState
    {
        get => _getReconcileState;
        set => _getReconcileState = value;
    }
    private Action _getReconcileState;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            GameObject.Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (ulong id) =>
            {
                if (NetworkManager.Singleton.ConnectedClients.Count == 2)
                {
                    SetRunningStateClientRPc(true);
                }
            };

            NetworkManager.Singleton.OnClientDisconnectCallback += (ulong id) =>
            {
                if (NetworkManager.Singleton.ConnectedClients.Count < 2)
                {
                    SetRunningStateClientRPc(false);
                }
            };
        }
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetRunningStateClientRPc(bool runningState)
    {
        _isRunning = runningState;
    }

    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        _timer += Time.deltaTime;

        while (_timer >= Time.fixedDeltaTime)
        {
            _timer -= Time.fixedDeltaTime;

            _getClientInput?.Invoke();

            if (IsServer)
            {
                _getServerInput?.Invoke();
            }

            Physics.Simulate(Time.fixedDeltaTime);

            if (IsServer)
            {
                _getServerState?.Invoke();
            }

            _getClientState?.Invoke();

            _currentTick++;
        }

        if (!IsServer)
        {
            _getReconcileCondition?.Invoke();

            if (_needReconcile)
            {
                _lastReconciledTick = _reconcileTick;
                _preReconcile?.Invoke(_reconcileTick);

                _reconcileTick++;

                while (_reconcileTick < _currentTick)
                {
                    _getReconcileInput.Invoke(_reconcileTick);
                    Physics.Simulate(Time.fixedDeltaTime);
                    _getReconcileState.Invoke();

                    _reconcileTick++;
                }

                _needReconcile = false;
            }
        }
    }

    void OnGUI()
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.black;

        GUI.Box(new Rect(45, 15, 410, 30), GUIContent.none);

        GUILayout.BeginArea(new Rect(50, 20, 400, 100));
        GUILayout.Label($"CurrentTick: {_currentTick}\t Reconcile Tick: {_reconcileTick}");
        GUILayout.EndArea();

        GUI.backgroundColor = originalColor;
    }
}
