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
				//.WithBurst()
				.WithoutBurst()
				.WithName("Update_Chunk")
				.ForEach((Entity entity, int entityInQueryIndex, ref ChunkComponent cc, ref DynamicBuffer<Mixed.Voxel> buffer, in UpdateChunkTag updateData, in LocalToParent pp) =>
				{
					barrier.RemoveComponent<UpdateChunkTag>(entityInQueryIndex, entity);

					var stencil = updateData.input;

					var stencilCenter = new float4(stencil.centerX, stencil.centerY, 0, 0);
					var translatedStencil = math.mul(pp.Value, stencilCenter);

					stencil.centerX = stencilCenter.x;
					stencil.centerY = stencilCenter.y;

					int xStartChunk = (int)(stencil.XStart / level.chunkSize);
					int xEndChunk = (int)(stencil.XEnd / level.chunkSize);
					int yStartChunk = (int)(stencil.YStart / level.chunkSize);
					int yEndChunk = (int)(stencil.YEnd / level.chunkSize);

					int xStart = (int)(stencil.XStart / level.voxelSize) - cc.x * LevelComponent.VoxelResolution;
					int xEnd = (int)((stencil.XEnd) / level.voxelSize) - cc.x * LevelComponent.VoxelResolution + 1;
					int yStart = (int)(stencil.YStart / level.voxelSize) - cc.y * LevelComponent.VoxelResolution;
					int yEnd = (int)(stencil.YEnd / level.voxelSize) - cc.y * LevelComponent.VoxelResolution;

					//Debug.Log($"xsc: ({cc.x}), ({xStart}-{xEnd})");
					//Debug.Log($"ysc: ({cc.y}), ({yStart}-{yEnd})");

					if (xStart < 0) xStart = 0;
					if (yStart < 0) yStart = 0;
					if (xEnd >= LevelComponent.VoxelResolution) xEnd = LevelComponent.VoxelResolution - 1;
					if (yEnd >= LevelComponent.VoxelResolution) yEnd = LevelComponent.VoxelResolution - 1;

					Debug.Log($"[{xStart},{xEnd}]-[{yStart},{yEnd}]");

					for (int y = 0; y < LevelComponent.VoxelResolution; y++)
					{
						var s = y * LevelComponent.VoxelResolution;
						for (int x = 0; x < LevelComponent.VoxelResolution; x++)
						{
							var i = s + x;
							float2 pos = buffer[i].position;
							if (stencil.InRange(pos, cc.x, cc.y, level.chunkSize))
							{
								buffer[i] = buffer[i].Copy(stencil.fillType);
							}
						}
					}
					/*for (int y = yStart; y <= yEnd; y++)
					{
						int i = (y + 1) * LevelComponent.VoxelResolution - xEnd - 1;
						for (int x = xStart; x <= xEnd; x++, i--)
						{

							float2 pos = buffer[i].position;
							if (stencil.InRange(pos, cc.x, cc.y, level.chunkSize))
							{
								buffer[i] = buffer[i].Copy(stencil.fillType);
							}
						}
					}*/
					//SetCrossings(stencil, xStart, xEnd, yStart, yEnd);

					barrier.AddComponent<TriangulateTag>(entityInQueryIndex, entity);
				}).Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(updateVoxelHandle);

			return updateVoxelHandle;
		}
	}


}
