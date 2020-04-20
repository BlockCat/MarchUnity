using March.Terrain.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace March.Terrain
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]	
	public class VoxelCollectSystem : SystemBase
	{
		struct CollectVoxelTag : IComponentData { }

		public NativeHashMap<ChunkComponent, NativeArray<Entity>> Chunks;
		public NativeHashMap<Entity, (Entity, Entity, Entity)> x_y_xy_Neighbours;
		private EntityQuery m_Voxels;

		protected override void OnCreate()
		{
			Chunks = new NativeHashMap<ChunkComponent, NativeArray<Entity>>(16, Allocator.Persistent);
			EntityManager.CreateEntity(typeof(CollectVoxelTag));
			RequireSingletonForUpdate<CollectVoxelTag>();
			m_Voxels = GetEntityQuery(ComponentType.ReadOnly<Voxel>());
			RequireForUpdate(m_Voxels);
		}
		protected override void OnUpdate()
		{
			EntityManager.DestroyEntity(GetSingletonEntity<CollectVoxelTag>());

			var level = GetSingleton<LevelComponent>();

			for (int x = 0; x < level.ChunkResolution; x++)
			{
				for (int y = 0; y < level.ChunkResolution; y++)
				{
					var chunkData = new ChunkComponent { x = x, y = y };
					var voxelArray = new NativeArray<Entity>(LevelComponent.VoxelResolution * LevelComponent.VoxelResolution, Allocator.Persistent);

					Entities
						.WithBurst()
						.WithStoreEntityQueryInField(ref m_Voxels)
						.WithNativeDisableContainerSafetyRestriction(voxelArray)
						.WithSharedComponentFilter(chunkData)
						.ForEach((Entity entity, in Voxel voxel) =>
						{
							voxelArray[voxel.index] = entity;
						}).ScheduleParallel();

#error todo: Neighbours


					Chunks.Add(chunkData, voxelArray);
				}
			}
		}

		protected override void OnDestroy()
		{
			var array = Chunks.GetValueArray(Allocator.Temp);
			foreach (var entry in array)
			{
				entry.Dispose();
			}
			array.Dispose();
			Chunks.Dispose();
		}
	}
}
