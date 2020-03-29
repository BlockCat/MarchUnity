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
			Debug.Log("Updating chunks");
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var level = GetSingleton<LevelComponent>();
			//var buffer = m_Barrier.GetBufferFromEntity<Mixed.Voxel>(false);
			var updateVoxelHandle = Entities
				.WithBurst()
				.WithName("Update_Chunk")
				.ForEach((Entity entity, int entityInQueryIndex, ref ChunkComponent cc, ref DynamicBuffer<Mixed.Voxel> buffer, in UpdateChunkTag updateData) =>
				{
					barrier.RemoveComponent<UpdateChunkTag>(entityInQueryIndex, entity);

					var stencil = updateData.input;

					int xStart = (int)(stencil.XStart / level.voxelSize);
					int xEnd = (int)(stencil.XEnd / level.voxelSize);
					int yStart = (int)(stencil.YStart / level.voxelSize);
					int yEnd = (int)(stencil.YEnd / level.voxelSize);

					if (xStart < 0) xStart = 0;
					if (xEnd >= LevelComponent.VoxelResolution) xEnd = LevelComponent.VoxelResolution - 1;
					if (yStart < 0) yStart = 0;
					if (yEnd >= LevelComponent.VoxelResolution) yEnd = LevelComponent.VoxelResolution - 1;

					for (int y = yStart; y <= yEnd; y++)
					{
						int i = y * LevelComponent.VoxelResolution + xStart;
						for (int x = xStart; x <= xEnd; x++, i++)
						{
							float2 pos = buffer[i].position;
							if (stencil.InRange(pos))
							{
								buffer[i].SetState(stencil.fillType);
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
