using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkSyncObject : NetworkBehaviour
{
    protected const int BUFFER_SIZE = 1024;

    protected InputPayload[] _inputBuffer = new InputPayload[BUFFER_SIZE];
    protected StatePayload[] _stateBuffer = new StatePayload[BUFFER_SIZE];

    protected Queue<InputPayload> _inputQueue = new Queue<InputPayload>();
    protected Queue<StatePayload> _stateQueue = new Queue<StatePayload>();

    protected InputPayload _processingInput;
    protected StatePayload _reconcileTarget;
    protected int _processingTick = 0;

    protected Rigidbody _rigidbody;

    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody>();

        NetworkSyncManager.Instance.GetClientInput += GetClientInput;
        NetworkSyncManager.Instance.GetClientState += GetClientState;

        if (IsServer)
        {
            if (!IsOwner) NetworkSyncManager.Instance.GetServerInput += GetServerInput;
            NetworkSyncManager.Instance.GetServerState += GetServerState;
        }
        else
        {
            NetworkSyncManager.Instance.GetReconcileCondition += GetReconcileCondition;
            NetworkSyncManager.Instance.PreReconcile += PreReconcile;
            NetworkSyncManager.Instance.GetReconcileInput += GetReconcileInput;
            NetworkSyncManager.Instance.GetReconcileState += GetReconcileState;
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkSyncManager.Instance.GetClientInput -= GetClientInput;
        NetworkSyncManager.Instance.GetClientState -= GetClientState;

        if (IsServer)
        {
            NetworkSyncManager.Instance.GetServerInput -= GetServerInput;
            NetworkSyncManager.Instance.GetServerState -= GetServerState;
        }
        else
        {
            NetworkSyncManager.Instance.GetReconcileCondition -= GetReconcileCondition;
            NetworkSyncManager.Instance.PreReconcile -= PreReconcile;
            NetworkSyncManager.Instance.GetReconcileInput -= GetReconcileInput;
            NetworkSyncManager.Instance.GetReconcileState -= GetReconcileState;
        }
    }

    public virtual bool GetInput()
    {
        return false;
    }

    public virtual void ApplyInput(InputPayload inputPayload)
    {
        return;
    }

    protected void GetClientInput()
    {
        if (GetInput())
        {
            ApplyInput(_processingInput);
            if (IsServer)
            {
                SendInputClientRpc(_processingInput);
            }
            else
            {
                SendInputServerRpc(_processingInput);
            }
        }
    }

    protected void GetClientState()
    {
        StatePayload statePayload = new StatePayload();

        statePayload.tick = NetworkSyncManager.Instance.CurrentTick;

        statePayload.position = _rigidbody.position;
        statePayload.rotation = _rigidbody.rotation;
        statePayload.velocity = _rigidbody.velocity;
        statePayload.angularVelocity = _rigidbody.angularVelocity;

        _stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
    }

    protected void GetServerInput()
    {
        while (_inputQueue.Count > 0)
        {
            _processingInput = _inputQueue.Dequeue();
            ApplyInput(_processingInput);
        }
    }

    protected void GetServerState()
    {
        StatePayload statePayload = new StatePayload();

        statePayload.tick = _processingInput.tick;

        statePayload.position = _rigidbody.position;
        statePayload.rotation = _rigidbody.rotation;
        statePayload.velocity = _rigidbody.velocity;
        statePayload.angularVelocity = _rigidbody.angularVelocity;

        SendStateClientRpc(statePayload);
    }

    protected void GetReconcileCondition()
    {
        while (_stateQueue.Count > 0)
        {
            StatePayload statePayload = _stateQueue.Dequeue();

            if (statePayload.tick < NetworkSyncManager.Instance.LastReconciledTick)
            {
                continue;
            }

            int bufferIndex = statePayload.tick % BUFFER_SIZE;

            if (_stateBuffer[bufferIndex].tick != statePayload.tick)
            {
                continue;
            }

            Vector3 error = statePayload.position - _stateBuffer[bufferIndex].position;

            if (error.sqrMagnitude > 0.00001f)
            {
                DebugManager.Instance.AddDebugText($"{statePayload.position.x:0.00} {statePayload.position.y:0.00} {statePayload.position.z:0.00} :: {_stateBuffer[bufferIndex].position.x:0.00} {_stateBuffer[bufferIndex].position.y:0.00} {_stateBuffer[bufferIndex].position.z:0.00}");
                if (!NetworkSyncManager.Instance.NeedReconcile)
                {
                    NetworkSyncManager.Instance.NeedReconcile = true;
                    NetworkSyncManager.Instance.ReconcileTick = statePayload.tick;

                    _reconcileTarget = statePayload;
                }
                else if (NetworkSyncManager.Instance.ReconcileTick > statePayload.tick)
                {
                    NetworkSyncManager.Instance.ReconcileTick = statePayload.tick;

                    _reconcileTarget = statePayload;
                }
            }
        }
    }

    protected void PreReconcile(int reconcileTick)
    {
        int bufferIndex = reconcileTick % BUFFER_SIZE;

        if (_reconcileTarget.tick == reconcileTick)
        {
            _rigidbody.position = _reconcileTarget.position;
            _rigidbody.rotation = _reconcileTarget.rotation;
            _rigidbody.velocity = _reconcileTarget.velocity;
            _rigidbody.angularVelocity = _reconcileTarget.angularVelocity;
        }
        else
        {
            if (_stateBuffer[bufferIndex].tick == reconcileTick)
            {
                _rigidbody.position = _stateBuffer[bufferIndex].position;
                _rigidbody.rotation = _stateBuffer[bufferIndex].rotation;
                _rigidbody.velocity = _stateBuffer[bufferIndex].velocity;
                _rigidbody.angularVelocity = _stateBuffer[bufferIndex].angularVelocity;
            }
        }
    }

    protected void GetReconcileInput(int reconcileTick)
    {
        int bufferIndex = reconcileTick % BUFFER_SIZE;

        if (reconcileTick == _inputBuffer[bufferIndex].tick)
        {
            _processingInput = _inputBuffer[bufferIndex];
            ApplyInput(_processingInput);
        }
    }

    protected void GetReconcileState()
    {
        StatePayload statePayload = new StatePayload();

        statePayload.tick = _processingInput.tick;

        statePayload.position = _rigidbody.position;
        statePayload.rotation = _rigidbody.rotation;
        statePayload.velocity = _rigidbody.velocity;
        statePayload.angularVelocity = _rigidbody.angularVelocity;

        _stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendInputServerRpc(InputPayload inputPayload) {
        _inputQueue.Enqueue(inputPayload);
        SendInputClientRpc(inputPayload);
    }

    [ClientRpc(RequireOwnership = false)]
    private void SendInputClientRpc(InputPayload inputPayload)
    {
        int bufferIndex = inputPayload.tick % BUFFER_SIZE;

        _inputBuffer[bufferIndex] = inputPayload;
    }

    [ClientRpc(RequireOwnership = false)]
    private void SendStateClientRpc(StatePayload statePayload)
    {
        _stateQueue.Enqueue(statePayload);
    }

    void OnGUI()
    {
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.black;

        GUIStyle customLabelStyle = new GUIStyle(GUI.skin.label);
        customLabelStyle.padding = new RectOffset(2, 2, 2, 2);
        customLabelStyle.margin = new RectOffset(0, 0, 0, 0);


        if (gameObject.TryGetComponent<PlayerController>(out PlayerController pc))
        {
            if (pc.PlayerColor.Value == ColorType.Red || pc.PlayerColor.Value == ColorType.Blue)
            {
                if (pc.PlayerColor.Value == ColorType.Red)
                {
                    GUI.Box(new Rect(45, 55, 260, 300), GUIContent.none);
                    GUILayout.BeginArea(new Rect(50, 60, 250, 290));

                    GUILayout.Label("Red:", customLabelStyle);
                }
                else if (pc.PlayerColor.Value == ColorType.Blue)
                {
                    GUI.Box(new Rect(315, 55, 260, 300), GUIContent.none);
                    GUILayout.BeginArea(new Rect(320, 60, 250, 290));
                    GUILayout.Label("Blue:");
                }
                GUILayout.Label($"Processing Tick: {_processingTick}");
                GUILayout.Label($"Reconcile Target: {_reconcileTarget.tick}");

                GUILayout.Label($"Input: \t\t State:");

                if (NetworkSyncManager.Instance.CurrentTick >= 10)
                {
                    for (int j = NetworkSyncManager.Instance.CurrentTick - 10; j < NetworkSyncManager.Instance.CurrentTick; j++)
                    {
                        int i = j % 1024;
                        GUILayout.Label($"{_inputBuffer[i].tick % 1000}: {_inputBuffer[i].inputVector.x:0.0} {_inputBuffer[i].inputVector.y:0.0} {_inputBuffer[i].inputVector.z:0.0} \t {_stateBuffer[i].tick % 1000}: {_stateBuffer[i].position.x:0.0} {_stateBuffer[i].position.y:0.0} {_stateBuffer[i].position.z:0.0}", customLabelStyle);
                    }
                }
                GUILayout.EndArea();
            }
        }

        GUI.backgroundColor = originalColor;
    }
}
