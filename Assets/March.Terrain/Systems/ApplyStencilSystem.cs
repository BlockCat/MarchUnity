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
			var levelEntity = GetSingletonEntity<LevelComponent>();
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var chunkEntities = m_ChunkGroup.ToEntityArrayAsync(Unity.Collections.Allocator.TempJob, out JobHandle groupHandle2);
			var chunkData = GetComponentDataFromEntity<ChunkComponent>(true);
			var localToWorld = GetComponentDataFromEntity<LocalToWorld>(true)[levelEntity];
			var handle = Entities
				.WithName("Apply_Stencil")
				.WithReadOnly(chunkData)
				.WithBurst()
				.WithDeallocateOnJobCompletion(chunkEntities)
				.ForEach((Entity entity, int entityInQueryIndex, in VoxelStencilInput inputData) =>
				{
					barrier.DestroyEntity(entityInQueryIndex, entity);

					var stencilPosition = new float4(inputData.centerX, inputData.centerY, 0, 1);
					var translatedStencilPosition = math.mul(math.inverse(localToWorld.Value), stencilPosition);

					var translatedData = new VoxelStencilInput
					{
						centerX = translatedStencilPosition.x,
						centerY = translatedStencilPosition.y,
						fillType = inputData.fillType,
						radius = inputData.radius,
						shape = inputData.shape,
						Tick = inputData.Tick
					};

					int xChunkStart = (int)((translatedData.XStart - 1) / level.chunkSize);
					int xChunkEnd = (int)((translatedData.XEnd + 1) / level.chunkSize);
					int yChunkStart = (int)((translatedData.YStart - 1) / level.chunkSize);
					int yChunkEnd = (int)((translatedData.YEnd + 1) / level.chunkSize);

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
							barrier.AddComponent(entityInQueryIndex, chunk, new UpdateChunkTag { input = translatedData });
						}
					}

					
				}).Schedule(JobHandle.CombineDependencies(groupHandle2, inputDeps));
			m_Barrier.AddJobHandleForProducer(handle);
			return handle;
		}
	}
}
