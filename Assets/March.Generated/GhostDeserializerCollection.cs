using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct MarchingSquaresGhostDeserializerCollection : IGhostDeserializerCollection
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
    public void Initialize(World world)
    {
        var curSphereGhostSpawnSystem = world.GetOrCreateSystem<SphereGhostSpawnSystem>();
        m_SphereSnapshotDataNewGhostIds = curSphereGhostSpawnSystem.NewGhostIds;
        m_SphereSnapshotDataNewGhosts = curSphereGhostSpawnSystem.NewGhosts;
        curSphereGhostSpawnSystem.GhostType = 0;
        var curVoxelGridGhostSpawnSystem = world.GetOrCreateSystem<VoxelGridGhostSpawnSystem>();
        m_VoxelGridSnapshotDataNewGhostIds = curVoxelGridGhostSpawnSystem.NewGhostIds;
        m_VoxelGridSnapshotDataNewGhosts = curVoxelGridGhostSpawnSystem.NewGhosts;
        curVoxelGridGhostSpawnSystem.GhostType = 1;
    }

    public void BeginDeserialize(JobComponentSystem system)
    {
        m_SphereSnapshotDataFromEntity = system.GetBufferFromEntity<SphereSnapshotData>();
        m_VoxelGridSnapshotDataFromEntity = system.GetBufferFromEntity<VoxelGridSnapshotData>();
    }
    public bool Deserialize(int serializer, Entity entity, uint snapshot, uint baseline, uint baseline2, uint baseline3,
        ref DataStreamReader reader, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
                return GhostReceiveSystem<MarchingSquaresGhostDeserializerCollection>.InvokeDeserialize(m_SphereSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, ref reader, compressionModel);
            case 1:
                return GhostReceiveSystem<MarchingSquaresGhostDeserializerCollection>.InvokeDeserialize(m_VoxelGridSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, ref reader, compressionModel);
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    public void Spawn(int serializer, int ghostId, uint snapshot, ref DataStreamReader reader,
        NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
                m_SphereSnapshotDataNewGhostIds.Add(ghostId);
                m_SphereSnapshotDataNewGhosts.Add(GhostReceiveSystem<MarchingSquaresGhostDeserializerCollection>.InvokeSpawn<SphereSnapshotData>(snapshot, ref reader, compressionModel));
                break;
            case 1:
                m_VoxelGridSnapshotDataNewGhostIds.Add(ghostId);
                m_VoxelGridSnapshotDataNewGhosts.Add(GhostReceiveSystem<MarchingSquaresGhostDeserializerCollection>.InvokeSpawn<VoxelGridSnapshotData>(snapshot, ref reader, compressionModel));
                break;
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }

    private BufferFromEntity<SphereSnapshotData> m_SphereSnapshotDataFromEntity;
    private NativeList<int> m_SphereSnapshotDataNewGhostIds;
    private NativeList<SphereSnapshotData> m_SphereSnapshotDataNewGhosts;
    private BufferFromEntity<VoxelGridSnapshotData> m_VoxelGridSnapshotDataFromEntity;
    private NativeList<int> m_VoxelGridSnapshotDataNewGhostIds;
    private NativeList<VoxelGridSnapshotData> m_VoxelGridSnapshotDataNewGhosts;
}
public struct EnableMarchingSquaresGhostReceiveSystemComponent : IComponentData
{}
public class MarchingSquaresGhostReceiveSystem : GhostReceiveSystem<MarchingSquaresGhostDeserializerCollection>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<EnableMarchingSquaresGhostReceiveSystemComponent>();
    }
}
