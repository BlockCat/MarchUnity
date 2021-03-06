﻿using March.Terrain.Authoring;
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

namespace March.Terrain
{

	public class UpdateChunkSystem : JobComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		

		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			
			RequireSingletonForUpdate<LevelComponent>();
			RequireForUpdate(GetEntityQuery(typeof(UpdateChunkTag), typeof(ChunkComponent)));
		}
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var level = GetSingleton<LevelComponent>();
			var voxelBuffer = m_Barrier.GetBufferFromEntity<VoxelBuffer>(false);
			var updateVoxelHandle = Entities
				.WithBurst()
				//.WithoutBurst()
				.WithNativeDisableParallelForRestriction(voxelBuffer)
				.WithName("Update_Chunk")
				.ForEach((Entity entity, int entityInQueryIndex, ref ChunkComponent cc, in UpdateChunkTag updateData, in LocalToParent pp) =>
				{
					barrier.RemoveComponent<UpdateChunkTag>(entityInQueryIndex, entity);
					var buffer = voxelBuffer[entity];
					var stencil = updateData.input;

					var stencilCenter = new float4(stencil.centerX, stencil.centerY, 0, 1);
					var translatedStencil = math.mul(math.inverse(pp.Value), stencilCenter);
					var xNeighbor = cc.leftNeighbour;
					var yNeighbor = cc.upNeighbour;

					stencil.centerX = translatedStencil.x;
					stencil.centerY = translatedStencil.y;

					{
						int xStart = (int)(stencil.XStart / level.voxelSize);
						int xEnd = (int)((stencil.XEnd) / level.voxelSize);
						int yStart = (int)(stencil.YStart / level.voxelSize);
						int yEnd = (int)(stencil.YEnd / level.voxelSize);

						if (xStart < 0) xStart = 0;
						if (yStart < 0) yStart = 0;
						if (xEnd >= LevelComponent.VoxelResolution) xEnd = LevelComponent.VoxelResolution - 1;
						if (yEnd >= LevelComponent.VoxelResolution) yEnd = LevelComponent.VoxelResolution - 1;

						for (int y = yStart; y <= yEnd; y++)
						{
							for (int x = xStart; x <= xEnd; x++)
							{
								int i = y * LevelComponent.VoxelResolution + x;
								float2 pos = buffer[i].Value.position;
								if (stencil.InRange(pos))
								{
									buffer[i] = buffer[i].Value.Copy(stencil.fillType);
									if (stencil.shape == VoxelShape.Rectangle)
									{
										handleSquareY(ref voxelBuffer, ref buffer, ref stencil, yNeighbor, x, y, i);
										handleSquareX(ref voxelBuffer, ref buffer, ref stencil, xNeighbor, x, y, i);
									}
									else
									{
										handleCircleX(ref voxelBuffer, ref buffer, ref stencil, xNeighbor, x, y, i);
										handleCircleY(ref voxelBuffer, ref buffer, ref stencil, yNeighbor, x, y, i);
									}

								}
							}
						}
					}
					barrier.AddComponent<TriangulateTag>(entityInQueryIndex, entity);
				}).Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(updateVoxelHandle);

			return updateVoxelHandle;
		}

		//[BurstCompile]
		private static void handleCircleX(ref BufferFromEntity<VoxelBuffer> voxelBuffer, ref DynamicBuffer<VoxelBuffer> buffer, ref VoxelStencilInput stencil, Entity xNeighbor, int x, int y, int i)
		{
			if (x > 0 && !stencil.InRange(buffer[i - 1].Value.position))
			{
				Voxel v1 = buffer[i];
				Voxel v2 = buffer[i - 1];

				var D = math.pow(stencil.radius, 2) - math.pow(v1.position.y - stencil.centerY, 2);
				if (D > 0)
				{
					var d = math.sqrt(D);
					var cx1 = stencil.centerX - d;
					Debug.Assert(v2.position.x < cx1 && cx1 < v1.position.x);
					if (v2.xEdge == float.MinValue || v2.xEdge > cx1)
						v2.xEdge = cx1;
				}
				else
				{
					v2.xEdge = float.MinValue;
				}
				buffer[i - 1] = v2;
			}
			if (x < LevelComponent.VoxelResolution - 1 && !stencil.InRange(buffer[i + 1].Value.position))
			{
				Voxel v1 = buffer[i + 1];
				Voxel v2 = buffer[i];

				var D = math.pow(stencil.radius, 2) - math.pow(v1.position.y - stencil.centerY, 2);
				if (D > 0)
				{
					var d = math.sqrt(D);
					var cx1 = stencil.centerX + d;
					Debug.Assert(v2.position.x < cx1 && cx1 < v1.position.x);
					if (v2.xEdge == float.MinValue || v2.xEdge < cx1)
						v2.xEdge = cx1;
				}
				else
				{
					v2.xEdge = float.MinValue;
				}
				buffer[i] = v2;
			}
			else if (x == LevelComponent.VoxelResolution - 1)
			{

			}
		}
		[BurstCompile]
		private static void handleCircleY(ref BufferFromEntity<VoxelBuffer> voxelBuffer, ref DynamicBuffer<VoxelBuffer> buffer, ref VoxelStencilInput stencil, Entity yNeighbor, int x, int y, int i)
		{
			if (y > 0 && !stencil.InRange(buffer[i - LevelComponent.VoxelResolution].Value.position))
			{
				Voxel v1 = buffer[i];
				Voxel v2 = buffer[i - LevelComponent.VoxelResolution];

				var D = math.sqrt(math.pow(stencil.radius, 2) - math.pow(v1.position.x - stencil.centerX, 2));

				if (D > 0)
				{
					var d = math.sqrt(D);
					var cy1 = stencil.centerY + d;
					if (v2.yEdge == float.MinValue || v2.yEdge > cy1)
						v2.yEdge = cy1;
				} else
				{
					v2.yEdge = float.MinValue;
				}
				buffer[i - LevelComponent.VoxelResolution] = v2;
			}
			else if (y == 0 && yNeighbor != Entity.Null)
			{
				
			}

			if (y < LevelComponent.VoxelResolution - 1 && !stencil.InRange(buffer[i + LevelComponent.VoxelResolution].Value.position))
			{
				Voxel v1 = buffer[i + LevelComponent.VoxelResolution];
				Voxel v2 = buffer[i];

				var D = math.sqrt(math.pow(stencil.radius, 2) - math.pow(v1.position.x - stencil.centerX, 2));

				if (D > 0)
				{
					var d = math.sqrt(D);
					var cy1 = stencil.centerY - d;
					if (v2.yEdge == float.MinValue || v2.yEdge < cy1)
						v2.yEdge = cy1;
				}
				else
				{
					v2.yEdge = float.MinValue;
				}
				buffer[i] = v2;
			}
		}

		[BurstCompile]
		private static void handleSquareX(ref BufferFromEntity<VoxelBuffer> voxelBuffer, ref DynamicBuffer<VoxelBuffer> buffer, ref VoxelStencilInput stencil, Entity xNeighbor, int x, int y, int i)
		{
			if (x > 0 && !stencil.InRange(buffer[i - 1].Value.position))
			{
				var v = buffer[i - 1];
				if (buffer[i - 1].Value.state != stencil.fillType)
				{
					if (v.Value.xEdge == float.MinValue || v.Value.xEdge > stencil.XStart)
						v.Value.xEdge = stencil.XStart;
				}
				else
				{
					v.Value.xEdge = float.MinValue;
				}
				buffer[i - 1] = v;
			}

			if (x < LevelComponent.VoxelResolution - 1 && !stencil.InRange(buffer[i + 1].Value.position))
			{
				var v = buffer[i];
				if (buffer[i + 1].Value.state != stencil.fillType)
				{
					if (v.Value.xEdge == float.MinValue || v.Value.xEdge < stencil.XEnd)
						v.Value.xEdge = stencil.XEnd;
				}
				else
				{
					v.Value.xEdge = float.MinValue;
				}
				buffer[i] = v;
			}
			else if (x == LevelComponent.VoxelResolution - 1 && xNeighbor != Entity.Null)
			{
				var voxel = buffer[i];
				var neighourBuffer = voxelBuffer[xNeighbor];
				var otherDude = neighourBuffer[y * LevelComponent.VoxelResolution];
				if (!stencil.InRange(otherDude.Value.position))
				{
					if (otherDude.Value.state != stencil.fillType)
					{
						if (voxel.Value.xEdge == float.MinValue || voxel.Value.xEdge < stencil.XEnd)
							voxel.Value.xEdge = stencil.XEnd;
					}
					else
					{
						voxel.Value.xEdge = float.MinValue;
					}
					buffer[i] = voxel;
				}
			}

		}

		[BurstCompile]
		private static void handleSquareY(ref BufferFromEntity<VoxelBuffer> voxelBuffer, ref DynamicBuffer<VoxelBuffer> buffer, ref VoxelStencilInput stencil, Entity yNeighbor, int x, int y, int i)
		{
			if (y > 0 && !stencil.InRange(buffer[i - LevelComponent.VoxelResolution].Value.position))
			{
				int index = i - LevelComponent.VoxelResolution;
				var v = buffer[index];//.Copy(buffer[index].state);
				if (buffer[index].Value.state != stencil.fillType)
				{
					if (v.Value.yEdge == float.MinValue || v.Value.yEdge > stencil.YStart)
						v.Value.yEdge = stencil.YStart;
				}
				else
				{
					v.Value.yEdge = float.MinValue;
				}
				buffer[index] = v;
			}
			else if (y == 0 && yNeighbor != Entity.Null)
			{
				var neighourBuffer = voxelBuffer[yNeighbor];
				var otherDude = neighourBuffer[LevelComponent.VoxelResolution * (LevelComponent.VoxelResolution - 1) + x];
				if (!stencil.InRange(otherDude.Value.position))
				{
					if (otherDude.Value.state != stencil.fillType)
					{
						if (otherDude.Value.yEdge == float.MinValue || otherDude.Value.yEdge > stencil.YStart)
							otherDude.Value.yEdge = stencil.YStart;
					}
					else
					{
						otherDude.Value.yEdge = float.MinValue;
					}
					neighourBuffer[LevelComponent.VoxelResolution * (LevelComponent.VoxelResolution - 1) + x] = otherDude;
				}
			}

			if (y < LevelComponent.VoxelResolution - 1 && !stencil.InRange(buffer[i + LevelComponent.VoxelResolution].Value.position))
			{
				int index = i + LevelComponent.VoxelResolution;
				var v = buffer[i].Value.Copy(buffer[i].Value.state);
				if (buffer[index].Value.state != stencil.fillType)
				{
					if (v.yEdge == float.MinValue || v.yEdge < stencil.YEnd)
						v.yEdge = stencil.YEnd;
				}
				else
				{
					v.yEdge = float.MinValue;
				}
				buffer[i] = v;
			}
		}
	}


}
