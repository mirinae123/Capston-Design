using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

// This example Payload file is slightly different from the readme file. 
// It gives a few more examples on how the pay loads can be modified to fit your needs

public struct PlayerInputPayload : ICompressible
{
    private int tick;
    private byte objectID;
    public Vector3 inputVector;
    public Vector3 subVector;
    private byte numberOfCopies;

    public int Tick { get => tick; set => this.tick = value; }

    public byte ObjectID { get => objectID; set => this.objectID = value; }

    public byte NumberOfCopies { get => numberOfCopies; set => this.numberOfCopies = value; }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref objectID);
        serializer.SerializeValue(ref inputVector);
        serializer.SerializeValue(ref subVector);
        serializer.SerializeValue(ref numberOfCopies);
    }
}

public struct PlayerStatePayload : IPayLoad
{
    private int tick;
    private byte objectID;
    public Vector3 position;
    public uint rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;

    public int Tick { get => tick; set => this.tick = value; }

    public byte ObjectID { get => objectID; set => this.objectID = value; }

    public Quaternion GetRot()
    {
        Quaternion rot = Quaternion.identity;
        QuaternionCompressor.DecompressQuaternion(ref rot, rotation);
        return rot;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref objectID);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
        serializer.SerializeValue(ref velocity);
        serializer.SerializeValue(ref angularVelocity);
    }
}

public struct WorldInputPayload : INetworkSerializable
{
    public int tick;
    public PlayerInputPayload[] inputs;
    public static WorldInputPayload Create(Message statemessage)
    {
        WorldInputPayload holder = new WorldInputPayload();
        holder.inputs = statemessage.inputs.ToArray();
        holder.tick = statemessage.tick;
        return holder;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref inputs);
        serializer.SerializeValue(ref tick);
    }
}

public struct WorldPayload : INetworkSerializable
{
    public int tick;
    public PlayerStatePayload[] states;
    public PlayerInputPayload[] inputs;
    public static WorldPayload Create(Message statemessage)
    {
        WorldPayload holder = new WorldPayload();
        holder.states = statemessage.states.ToArray();
        holder.inputs = statemessage.inputs.ToArray();
        holder.tick = statemessage.tick;
        return holder;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref states);
        serializer.SerializeValue(ref inputs);
        serializer.SerializeValue(ref tick);
    }
}

public class Message : EventArgs
{
    public List<PlayerStatePayload> states;
    public List<PlayerInputPayload> inputs;
    public int tick;
    public ushort OwnerID;
    public ClientRpcParams sendParams;

    public Message()
    {
        states = new List<PlayerStatePayload>();
        inputs = new List<PlayerInputPayload>();
        tick = 0;
        OwnerID = 0;
    }

    public Message(WorldPayload worldPayload)
    {
        this.states = new List<PlayerStatePayload>(worldPayload.states);
        this.inputs = new List<PlayerInputPayload>(worldPayload.inputs);
        this.tick = worldPayload.tick;
    }

    public Message(WorldInputPayload worldPayload)
    {
        this.inputs = new List<PlayerInputPayload>(worldPayload.inputs);
        this.tick = worldPayload.tick;
    }
}

public interface IPayLoad : INetworkSerializable
{
    int Tick { get; set; }

    byte ObjectID { get; set; }
}

public interface ICompressible : IPayLoad
{
    byte NumberOfCopies { get; set; }
}