using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkSyncManager : NetworkBehaviour
{
    public static NetworkSyncManager Instance;

    private float _timer = 0f;

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

    public Action<int> Reconcile
    {
        get => _reconcile;
        set => _reconcile = value;
    }
    private Action<int> _reconcile;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            GameObject.Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
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

        //if (!IsServer)
        //{
        //    _getReconcileCondition?.Invoke();

        //    if (_needReconcile)
        //    {
        //        _preReconcile?.Invoke(_reconcileTick);

        //        _reconcileTick++;

        //        while (_reconcileTick < _currentTick)
        //        {
        //            _reconcile?.Invoke(_reconcileTick);
        //            Physics.Simulate(Time.fixedDeltaTime);
        //            _reconcileTick++;
        //        }

        //        _needReconcile = false;
        //    }
        //}
    }
}
