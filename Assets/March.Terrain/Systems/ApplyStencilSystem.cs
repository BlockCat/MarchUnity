using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
			RequireForUpdate(GetEntityQuery(typeof(VoxelStencilInput)));
			RequireForUpdate(m_ChunkGroup);
		}


		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var level = GetSingleton<LevelComponent>();
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var chunkEntities = m_ChunkGroup.ToEntityArrayAsync(Unity.Collections.Allocator.TempJob, out JobHandle groupHandle2);
			var chunkData = GetComponentDataFromEntity<ChunkComponent>(true);
			var handle = Entities
				.WithName("Apply_Stencil")
				.WithReadOnly(chunkData)
				.WithBurst()
				.WithDeallocateOnJobCompletion(chunkEntities)
				.ForEach((Entity entity, int entityInQueryIndex, in VoxelStencilInput inputData) =>
				{
					barrier.DestroyEntity(entityInQueryIndex, entity);

					int xChunkStart = (int)((inputData.XStart) / level.chunkSize);
					int xChunkEnd = (int)((inputData.XEnd) / level.chunkSize);
					int yChunkStart = (int)((inputData.YStart) / level.chunkSize);
					int yChunkEnd = (int)((inputData.YEnd) / level.chunkSize);

					if (xChunkStart < 0) xChunkStart = 0;
					if (xChunkEnd >= level.ChunkResolution) xChunkEnd = level.ChunkResolution - 1;
					if (yChunkStart < 0) yChunkStart = 0;
					if (yChunkEnd >= level.ChunkResolution) yChunkEnd = level.ChunkResolution - 1;

					for (int i = 0; i < chunkEntities.Length; i++)
					{
						var chunk = chunkEntities[i];
						var data = chunkData[chunk];

						if (data.x >= xChunkStart && data.x <= xChunkEnd && data.y >= yChunkStart && data.y <= yChunkEnd)
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
