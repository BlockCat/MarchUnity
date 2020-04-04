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
			var voxelBuffer = m_Barrier.GetBufferFromEntity<Mixed.Voxel>(false);
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
								float2 pos = buffer[i].position;
								if (stencil.InRange(pos))
								{
									buffer[i] = buffer[i].Copy(stencil.fillType);


									if (y > 0 && !stencil.InRange(buffer[i - LevelComponent.VoxelResolution].position))
									{
										int index = i - LevelComponent.VoxelResolution;
										var v = buffer[index].Copy(buffer[index].state);
										if (buffer[index].state != stencil.fillType)
										{
											if (v.yEdge == float.MinValue || v.yEdge > stencil.YStart)
												v.yEdge = stencil.YStart;
										}
										else
										{
											v.yEdge = float.MinValue;
										}
										buffer[index] = v;
									}
									else if (y == 0 && yNeighbor != Entity.Null)
									{
										var neighourBuffer = voxelBuffer[yNeighbor];
										var otherDude = neighourBuffer[LevelComponent.VoxelResolution * (LevelComponent.VoxelResolution - 1) + x];
										if (!stencil.InRange(otherDude.position))
										{
											if (otherDude.state != stencil.fillType)
											{
												if (otherDude.yEdge == float.MinValue || otherDude.yEdge > stencil.YStart)
													otherDude.yEdge = stencil.YStart;
											}
											else
											{
												otherDude.yEdge = float.MinValue;
											}
											neighourBuffer[LevelComponent.VoxelResolution * (LevelComponent.VoxelResolution - 1) + x] = otherDude;
										}
									}

									if (y < LevelComponent.VoxelResolution - 1 && !stencil.InRange(buffer[i + LevelComponent.VoxelResolution].position))
									{
										int index = i + LevelComponent.VoxelResolution;
										var v = buffer[i].Copy(buffer[i].state);
										if (buffer[index].state != stencil.fillType)
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

									if (x > 0 && !stencil.InRange(buffer[i - 1].position))
									{
										var v = buffer[i - 1].Copy(buffer[i - 1].state);
										if (buffer[i - 1].state != stencil.fillType)
										{
											if (v.xEdge == float.MinValue || v.xEdge > stencil.XStart)
												v.xEdge = stencil.XStart;
										}
										else
										{
											v.xEdge = float.MinValue;
										}
										buffer[i - 1] = v;
									}
									else if (x == 0 && xNeighbor != Entity.Null)
									{
										var neighourBuffer = voxelBuffer[xNeighbor];
										var otherDude = neighourBuffer[(y + 1) * LevelComponent.VoxelResolution - 1];
										if (!stencil.InRange(otherDude.position))
										{
											if (otherDude.state != stencil.fillType)
											{
												if (otherDude.xEdge == float.MinValue || otherDude.xEdge > stencil.XStart)
													otherDude.xEdge = stencil.XStart;
											}
											else
											{
												otherDude.xEdge = float.MinValue;
											}
											neighourBuffer[(y + 1) * LevelComponent.VoxelResolution - 1] = otherDude;
										}
									}

									if (x < LevelComponent.VoxelResolution - 1 && !stencil.InRange(buffer[i + 1].position))
									{
										var v = buffer[i].Copy(buffer[i].state);
										if (buffer[i + 1].state != stencil.fillType)
										{
											if (v.xEdge == float.MinValue || v.xEdge < stencil.XEnd)
												v.xEdge = stencil.XEnd;
										}
										else
										{
											v.xEdge = float.MinValue;
										}
										buffer[i] = v;
									}

								}
							}
						}

						// if (xEnd >= 0 && yEnd >= 0 && xEnd >= xStart && yEnd >= yStart)
						// SetCrossings(xStart, xEnd, yStart, yEnd, ref buffer);
					}

					//void SetCrossings(int xStart, int xEnd, int yStart, int yEnd, ref DynamicBuffer<Voxel> voxels)
					//{
					//	bool crossHorizontalGap = false;
					//	bool lastVerticalRow = false;
					//	bool crossVerticalGap = false;

					//	if (xStart > 0)
					//	{
					//		xStart -= 1;
					//	}
					//	if (xEnd == LevelComponent.VoxelResolution - 1)
					//	{
					//		xEnd -= 1;
					//		crossHorizontalGap = xNeighbor != null;
					//	}
					//	if (yStart > 0)
					//	{
					//		yStart -= 1;
					//	}
					//	if (yEnd == LevelComponent.VoxelResolution - 1)
					//	{
					//		yEnd -= 1;
					//		lastVerticalRow = true;
					//		crossVerticalGap = yNeighbor != null;
					//	}

					//	Voxel a, b;
					//	for (int y = yStart; y <= yEnd; y++)
					//	{
					//		int i = y * LevelComponent.VoxelResolution + xStart;
					//		b = voxels[i];
					//		for (int x = xStart; x <= xEnd; x++, i++)
					//		{
					//			a = b;
					//			b = voxels[i + 1];
					//			SetHorizontalCrossing(a, b);
					//			SetVerticalCrossing(a, voxels[i + LevelComponent.VoxelResolution]);
					//		}
					//		SetVerticalCrossing(b, voxels[i + LevelComponent.VoxelResolution]);
					//		if (crossHorizontalGap && xNeighbor != Entity.Null)
					//		{
					//			var dummyX = voxelBuffer[xNeighbor][y * LevelComponent.VoxelResolution].CopyDummyX(level.chunkSize);
					//			SetHorizontalCrossing(b, dummyX);
					//		}
					//	}

					//	if (lastVerticalRow)
					//	{
					//		int i = voxels.Length - LevelComponent.VoxelResolution + xStart;
					//		b = voxels[i];
					//		for (int x = xStart; x <= xEnd; x++, i++)
					//		{
					//			a = b;
					//			b = voxels[i + 1];
					//			SetHorizontalCrossing(a, b);
					//			if (crossVerticalGap)
					//			{
					//				var dummyY = voxelBuffer[yNeighbor][x].CopyDummyY(level.chunkSize);
					//				SetVerticalCrossing(a, dummyY);
					//			}
					//		}
					//		if (crossVerticalGap)
					//		{
					//			var dummyY = voxelBuffer[yNeighbor][xEnd + 1].CopyDummyY(level.chunkSize);
					//			SetVerticalCrossing(b, dummyY);
					//		}
					//		if (crossHorizontalGap)
					//		{
					//			var dummyX = voxelBuffer[xNeighbor][voxels.Length - LevelComponent.VoxelResolution].CopyDummyX(level.chunkSize);
					//			SetHorizontalCrossing(b, dummyX);
					//		}
					//	}
					//}

					//void SetHorizontalCrossing(Voxel xMin, Voxel xMax)
					//{
					//	if (xMin.state != xMax.state)
					//	{
					//		FindHorizontalCrossing(xMin, xMax);
					//	}
					//	else
					//	{
					//		xMin.xEdge = float.MinValue;
					//	}
					//}

					//void FindHorizontalCrossing(Voxel xMin, Voxel xMax)
					//{
					//	/*if (xMin.position.y < stencil.YStart || xMin.position.y > stencil.YEnd)
					//		return;

					//		var sten = stencil;
					//	if (xMin.state == stencil.fillType)
					//	{
					//		if (xMin.position.x <= sten.XEnd && xMax.position.x >= stencil.XEnd)
					//		{
					//			if (xMin.xEdge == float.MinValue || xMin.xEdge < stencil.XEnd)
					//			{
					//				xMin.xEdge = stencil.XEnd;
					//				xMin.xNormal = new float2(stencil.fillType ? 1f : -1f, 0f);
					//			}
					//		}
					//	}
					//	else if (xMax.state == stencil.fillType)
					//	{
					//		if (xMin.position.x <= stencil.XStart && xMax.position.x >= stencil.XStart)
					//		{
					//			if (xMin.xEdge == float.MinValue || xMin.xEdge > stencil.XStart)
					//			{
					//				xMin.xEdge = stencil.XStart;
					//				xMin.xNormal = new float2(stencil.fillType ? -1f : 1f, 0f);
					//			}
					//		}
					//	}*/
					//	if (xMax.position.y < stencil.YStart || xMax.position.y > stencil.YEnd)
					//		return;

					//	var sten = stencil;
					//	if (xMax.state == stencil.fillType)
					//	{
					//		if (xMax.position.x <= sten.XEnd && xMin.position.x >= stencil.XEnd)
					//		{
					//			if (xMax.xEdge == float.MinValue || xMax.xEdge < stencil.XEnd)
					//			{
					//				xMax.xEdge = stencil.XEnd;
					//				xMax.xNormal = new float2(stencil.fillType ? 1f : -1f, 0f);
					//			}
					//		}
					//	}
					//	else if (xMin.state == stencil.fillType)
					//	{
					//		if (xMax.position.x <= stencil.XStart && xMin.position.x >= stencil.XStart)
					//		{
					//			if (xMax.xEdge == float.MinValue || xMax.xEdge > stencil.XStart)
					//			{
					//				xMax.xEdge = stencil.XStart;
					//				xMax.xNormal = new float2(stencil.fillType ? -1f : 1f, 0f);
					//			}
					//		}
					//	}
					//}

					//void SetVerticalCrossing(Voxel yMin, Voxel yMax)
					//{
					//	if (yMin.state != yMax.state)
					//	{
					//		FindVerticalCrossing(yMin, yMax);
					//	}
					//	else
					//	{
					//		yMin.yEdge = float.MinValue;
					//	}
					//}


					//void FindVerticalCrossing(Voxel yMin, Voxel yMax)
					//{
					//	if (yMin.position.x < stencil.XStart || yMin.position.x > stencil.XEnd)
					//	{
					//		return;
					//	}
					//	if (yMin.state == stencil.fillType)
					//	{
					//		if (yMin.position.y <= stencil.YEnd && yMax.position.y >= stencil.YEnd)
					//		{
					//			if (yMin.yEdge == float.MinValue || yMin.yEdge < stencil.YEnd)
					//			{
					//				yMin.yEdge = stencil.YEnd;
					//				yMin.yNormal = new float2(0f, stencil.fillType ? 1f : -1f);
					//			}
					//		}
					//	}
					//	else if (yMax.state == stencil.fillType)
					//	{
					//		if (yMin.position.y <= stencil.YStart && yMax.position.y >= stencil.YStart)
					//		{
					//			if (yMin.yEdge == float.MinValue || yMin.yEdge > stencil.YStart)
					//			{
					//				yMin.yEdge = stencil.YStart;
					//				yMin.yNormal = new float2(0f, stencil.fillType ? -1f : 1f);
					//			}
					//		}
					//	}
					//}

					barrier.AddComponent<TriangulateTag>(entityInQueryIndex, entity);
				}).Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(updateVoxelHandle);

			return updateVoxelHandle;
		}
	}


}
