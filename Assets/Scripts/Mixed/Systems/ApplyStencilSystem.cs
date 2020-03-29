using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

namespace Mixed
{
	class ApplyStencilSystem : JobComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery m_ChunkGroup;

		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_ChunkGroup = GetEntityQuery(ComponentType.ReadOnly<ChunkComponent>());
			RequireSingletonForUpdate<LevelComponent>();
			RequireSingletonForUpdate<VoxelStencilInput>();
			RequireForUpdate(m_ChunkGroup);
		}

		[BurstCompile]
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var level = GetSingleton<LevelComponent>();
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var chunkEntities = m_ChunkGroup.ToEntityArrayAsync(Unity.Collections.Allocator.TempJob, out JobHandle groupHandle2);
			var chunkData = GetComponentDataFromEntity<ChunkComponent>(true);
			var handle = Entities
				.WithName("Apply_Stencil")
				.WithDeallocateOnJobCompletion(chunkEntities)
				.ForEach((Entity entity, int entityInQueryIndex, in VoxelStencilInput inputData) =>
				{
					barrier.DestroyEntity(entityInQueryIndex, entity);
					int xStart = (int)((inputData.XStart - level.voxelSize) / level.chunkSize);
					int xEnd = (int)((inputData.XEnd + level.voxelSize) / level.chunkSize);
					int yStart = (int)((inputData.YStart - level.voxelSize) / level.chunkSize);
					int yEnd = (int)((inputData.YEnd + level.voxelSize) / level.chunkSize);

					if (xStart < 0) xStart = 0;
					if (xEnd >= level.ChunkResolution) xEnd = level.ChunkResolution - 1;
					if (yStart < 0) yStart = 0;
					if (yEnd >= level.ChunkResolution) yEnd = level.ChunkResolution - 1;

					foreach (var chunk in chunkEntities)
					{
						var data = chunkData[chunk];
						if (data.x >= xStart && data.x <= xEnd && data.y >= yStart && data.y <= yEnd)
						{
							barrier.AddComponent(entityInQueryIndex, chunk, new UpdateChunkTag { input = inputData });
						}
					}
				}).Schedule(JobHandle.CombineDependencies(groupHandle2, inputDeps));
			m_Barrier.AddJobHandleForProducer(handle);
			return handle;
		}
	}
}
