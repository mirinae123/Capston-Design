using Unity.Netcode;
using UnityEngine;

public struct InputPayload : INetworkSerializable
{
    public int tick;

    public Vector3 move;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref tick);
        serializer.SerializeValue(ref move);
    }
}
