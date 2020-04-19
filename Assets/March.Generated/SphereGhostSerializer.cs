using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Assets.March.Player.Authoring;
using Unity.Physics;
using Unity.Transforms;

public struct SphereGhostSerializer : IGhostSerializer<SphereSnapshotData>
{
    private ComponentType componentTypeMovablePlayerComponent;
    private ComponentType componentTypePlayerTag;
    private ComponentType componentTypeRequestFreeze;
    private ComponentType componentTypePhysicsCollider;
    private ComponentType componentTypePhysicsDamping;
    private ComponentType componentTypePhysicsGravityFactor;
    private ComponentType componentTypePhysicsMass;
    private ComponentType componentTypePhysicsVelocity;
    private ComponentType componentTypeCompositeScale;
    private ComponentType componentTypeLocalToWorld;
    private ComponentType componentTypeRotation;
    private ComponentType componentTypeTranslation;
    // FIXME: These disable safety since all serializers have an instance of the same type - causing aliasing. Should be fixed in a cleaner way
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<MovablePlayerComponent> ghostMovablePlayerComponentType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Rotation> ghostRotationType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Translation> ghostTranslationType;


    public int CalculateImportance(ArchetypeChunk chunk)
    {
        return 1;
    }

    public int SnapshotSize => UnsafeUtility.SizeOf<SphereSnapshotData>();
    public void BeginSerialize(ComponentSystemBase system)
    {
        componentTypeMovablePlayerComponent = ComponentType.ReadWrite<MovablePlayerComponent>();
        componentTypePlayerTag = ComponentType.ReadWrite<PlayerTag>();
        componentTypeRequestFreeze = ComponentType.ReadWrite<RequestFreeze>();
        componentTypePhysicsCollider = ComponentType.ReadWrite<PhysicsCollider>();
        componentTypePhysicsDamping = ComponentType.ReadWrite<PhysicsDamping>();
        componentTypePhysicsGravityFactor = ComponentType.ReadWrite<PhysicsGravityFactor>();
        componentTypePhysicsMass = ComponentType.ReadWrite<PhysicsMass>();
        componentTypePhysicsVelocity = ComponentType.ReadWrite<PhysicsVelocity>();
        componentTypeCompositeScale = ComponentType.ReadWrite<CompositeScale>();
        componentTypeLocalToWorld = ComponentType.ReadWrite<LocalToWorld>();
        componentTypeRotation = ComponentType.ReadWrite<Rotation>();
        componentTypeTranslation = ComponentType.ReadWrite<Translation>();
        ghostMovablePlayerComponentType = system.GetArchetypeChunkComponentType<MovablePlayerComponent>(true);
        ghostRotationType = system.GetArchetypeChunkComponentType<Rotation>(true);
        ghostTranslationType = system.GetArchetypeChunkComponentType<Translation>(true);
    }

    public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref SphereSnapshotData snapshot, GhostSerializerState serializerState)
    {
        snapshot.tick = tick;
        var chunkDataMovablePlayerComponent = chunk.GetNativeArray(ghostMovablePlayerComponentType);
        var chunkDataRotation = chunk.GetNativeArray(ghostRotationType);
        var chunkDataTranslation = chunk.GetNativeArray(ghostTranslationType);
        snapshot.SetMovablePlayerComponentPlayerId(chunkDataMovablePlayerComponent[ent].PlayerId, serializerState);
        snapshot.SetRotationValue(chunkDataRotation[ent].Value, serializerState);
        snapshot.SetTranslationValue(chunkDataTranslation[ent].Value, serializerState);
    }
}
