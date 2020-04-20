using March.Terrain.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace March.Terrain
{
	class ApplyStencilSystem : SystemBase
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery m_ChunkGroup;
		private EntityQuery m_StencilGroup;
		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_ChunkGroup = GetEntityQuery(ComponentType.ReadOnly<ChunkComponent>());
			m_StencilGroup = GetEntityQuery(ComponentType.ReadOnly<VoxelStencilInput>());
			RequireSingletonForUpdate<LevelComponent>();
			RequireForUpdate(m_StencilGroup);
			RequireForUpdate(m_ChunkGroup);
		}


		protected override void OnUpdate()
		{
			var level = GetSingleton<LevelComponent>();
			var levelEntity = GetSingletonEntity<LevelComponent>();
			//var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var localToWorld = GetComponentDataFromEntity<LocalToWorld>(true)[levelEntity];

			// collect stencils
			var stencils = m_StencilGroup.ToComponentDataArray<VoxelStencilInput>(Unity.Collections.Allocator.Temp);
			int stencilCount = m_StencilGroup.CalculateEntityCount();

			var chunkData = new HashSet<(int, int, VoxelStencilInput)>();
			var neighbouringData = World.GetExistingSystem<VoxelCollectSystem>().x_y_xy_Neighbours;

			foreach (var stencil in stencils)
			{
				var stencilPosition = new float4(stencil.centerX, stencil.centerY, 0, 1);
				var translatedStencilPosition = math.mul(math.inverse(localToWorld.Value), stencilPosition);
				var translatedData = new VoxelStencilInput
				{
					centerX = translatedStencilPosition.x,
					centerY = translatedStencilPosition.y,
					fillType = stencil.fillType,
					radius = stencil.radius,
					shape = stencil.shape,
					Tick = stencil.Tick
				};
				int xChunkStart = (int)((translatedData.XStart - 1) / level.chunkSize);
				int xChunkEnd = (int)((translatedData.XEnd + 1) / level.chunkSize);
				int yChunkStart = (int)((translatedData.YStart - 1) / level.chunkSize);
				int yChunkEnd = (int)((translatedData.YEnd + 1) / level.chunkSize);

				if (xChunkStart < 0) xChunkStart = 0;
				if (xChunkEnd >= level.ChunkResolution) xChunkEnd = level.ChunkResolution - 1;
				if (yChunkStart < 0) yChunkStart = 0;
				if (yChunkEnd >= level.ChunkResolution) yChunkEnd = level.ChunkResolution - 1;

				for (int y = yChunkStart; y <= yChunkEnd; y++)
				{
					for (int x = xChunkStart; x <= xChunkEnd; x++)
					{						
						Entities
							.WithName("Apply_Stencil")
							.WithBurst()
							.WithReadOnly(chunkData)
							.WithReadOnly(neighbouringData)
							.WithSharedComponentFilter(new ChunkComponent { x = x, y = y })
							.ForEach((Entity entity, ref Voxel voxel, in Translation position) =>
							{
								var (xNeighbour, yNeighbour, _) = neighbouringData[entity];
#error TODO: handle this eleganty.
								if (stencil.InRange(in position))
								{
									voxel.state = stencil.fillType;
								}
							})
							.ScheduleParallel();
					}
				}
			}

			/*Entities
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


				}).Schedule(JobHandle.CombineDependencies(groupHandle2, inputDeps));*/
			m_Barrier.AddJobHandleForProducer(Dependency);

		}
	}
}
