using Unity.Netcode;
using UnityEngine;

public struct InputPayload : INetworkSerializable
{
    public NetworkObjectReference gameObject;
    public int tick;

    public Vector3 inputVector;
    public Vector3 subVector;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref gameObject);
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref inputVector);
        serializer.SerializeValue(ref subVector);
    }
}
