using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct MarchingSquaresGhostSerializerCollection : IGhostSerializerCollection
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public string[] CreateSerializerNameList()
    {
        var arr = new string[]
        {
            "SphereGhostSerializer",
            "VoxelGridGhostSerializer",
        };
        return arr;
    }

    public int Length => 2;
#endif
    public static int FindGhostType<T>()
        where T : struct, ISnapshotData<T>
    {
        if (typeof(T) == typeof(SphereSnapshotData))
            return 0;
        if (typeof(T) == typeof(VoxelGridSnapshotData))
            return 1;
        return -1;
    }

    public void BeginSerialize(ComponentSystemBase system)
    {
        m_SphereGhostSerializer.BeginSerialize(system);
        m_VoxelGridGhostSerializer.BeginSerialize(system);
    }

    public int CalculateImportance(int serializer, ArchetypeChunk chunk)
    {
        switch (serializer)
        {
            case 0:
                return m_SphereGhostSerializer.CalculateImportance(chunk);
            case 1:
                return m_VoxelGridGhostSerializer.CalculateImportance(chunk);
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int GetSnapshotSize(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_SphereGhostSerializer.SnapshotSize;
            case 1:
                return m_VoxelGridGhostSerializer.SnapshotSize;
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int Serialize(ref DataStreamWriter dataStream, SerializeData data)
    {
        switch (data.ghostType)
        {
            case 0:
            {
                return GhostSendSystem<MarchingSquaresGhostSerializerCollection>.InvokeSerialize<SphereGhostSerializer, SphereSnapshotData>(m_SphereGhostSerializer, ref dataStream, data);
            }
            case 1:
            {
                return GhostSendSystem<MarchingSquaresGhostSerializerCollection>.InvokeSerialize<VoxelGridGhostSerializer, VoxelGridSnapshotData>(m_VoxelGridGhostSerializer, ref dataStream, data);
            }
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    private SphereGhostSerializer m_SphereGhostSerializer;
    private VoxelGridGhostSerializer m_VoxelGridGhostSerializer;
}

public struct EnableMarchingSquaresGhostSendSystemComponent : IComponentData
{}
public class MarchingSquaresGhostSendSystem : GhostSendSystem<MarchingSquaresGhostSerializerCollection>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<EnableMarchingSquaresGhostSendSystemComponent>();
    }
}
