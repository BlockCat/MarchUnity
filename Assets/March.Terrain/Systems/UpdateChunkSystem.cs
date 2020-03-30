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
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Mixed
{

	public class UpdateChunkSystem : JobComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery m_UpdateGroup;

		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_UpdateGroup = GetEntityQuery(typeof(UpdateChunkTag), typeof(ChunkComponent));
			RequireSingletonForUpdate<LevelComponent>();
			RequireForUpdate(m_UpdateGroup);
		}
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var level = GetSingleton<LevelComponent>();
			//var buffer = m_Barrier.GetBufferFromEntity<Mixed.Voxel>(false);
			var updateVoxelHandle = Entities
				.WithBurst()
				//.WithoutBurst()
				.WithName("Update_Chunk")
				.ForEach((Entity entity, int entityInQueryIndex, ref ChunkComponent cc, ref DynamicBuffer<Mixed.Voxel> buffer, in UpdateChunkTag updateData, in LocalToParent pp) =>
				{
					barrier.RemoveComponent<UpdateChunkTag>(entityInQueryIndex, entity);

					var stencil = updateData.input;

					var stencilCenter = new float4(stencil.centerX, stencil.centerY, 0, 1);
					var translatedStencil = math.mul(math.inverse(pp.Value), stencilCenter);

					stencil.centerX = translatedStencil.x;
					stencil.centerY = translatedStencil.y;

			
					int xStart = (int)(stencil.XStart / level.voxelSize) - 1;
					int xEnd = (int)((stencil.XEnd) / level.voxelSize) + 1;
					int yStart = (int)(stencil.YStart / level.voxelSize) - 1;
					int yEnd = (int)(stencil.YEnd / level.voxelSize) + 1;

					if (xStart < 0) xStart = 0;
					if (yStart < 0) yStart = 0;
					if (xEnd >= LevelComponent.VoxelResolution) xEnd = LevelComponent.VoxelResolution - 1;
					if (yEnd >= LevelComponent.VoxelResolution) yEnd = LevelComponent.VoxelResolution - 1;

					for (int y = yStart; y <= yEnd; y++)
					{
						for (int x = xStart; x <= xEnd; x++)
						{
							int i = y * LevelComponent.VoxelResolution + x;
							float2 pos = buffer[i].position;
							if (stencil.InRange(pos))
							{
								buffer[i] = buffer[i].Copy(stencil.fillType);
							}
						}
					}
					//SetCrossings(stencil, xStart, xEnd, yStart, yEnd);

					barrier.AddComponent<TriangulateTag>(entityInQueryIndex, entity);
				}).Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(updateVoxelHandle);

			return updateVoxelHandle;
		}
	}


}
