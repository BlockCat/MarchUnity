using March.Terrain.Authoring;
using System.Collections.Generic;
using System.Linq;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;

namespace March.Terrain
{
	[InternalBufferCapacity(LevelComponent.VoxelResolution * LevelComponent.VoxelResolution * 3)]
	struct IntBuffer : IBufferElementData
	{
		public int Value;
		public static implicit operator int(IntBuffer e) => e.Value;
		public static implicit operator IntBuffer(int e) => new IntBuffer { Value = e };
	}

	[InternalBufferCapacity(200)]
	struct VectorBuffer : IBufferElementData
	{
		public float3 Value;
		public static implicit operator float3(VectorBuffer e) => e.Value;
		public static implicit operator Vector3(VectorBuffer e) => e.Value;
		public static implicit operator VectorBuffer(float2 e) => new VectorBuffer { Value = new float3(e.x, e.y, 0) };
		public static implicit operator VectorBuffer(float3 e) => new VectorBuffer { Value = e };
		public static implicit operator VectorBuffer(Vector3 e) => new VectorBuffer { Value = e };
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]	
	public class TriangulateChunkSystem : SystemBase
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private VoxelCollectSystem m_VoxelSystem;

		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_VoxelSystem = World.GetOrCreateSystem<VoxelCollectSystem>();
			RequireForUpdate(GetEntityQuery(typeof(TriangulateTag)));
			RequireSingletonForUpdate<LevelComponent>();
		}


		#region triangulation

		[BurstCompile]
		private static void AddTriangle(int a, int b, int c, ref DynamicBuffer<IntBuffer> triangles)
		{
			triangles.Add(c);
			triangles.Add(b);
			triangles.Add(a);
		}
		[BurstCompile]
		private static void AddQuad(int a, int b, int c, int d, ref DynamicBuffer<IntBuffer> triangles)
		{
			triangles.Add(c);
			triangles.Add(b);
			triangles.Add(a);
			triangles.Add(d);
			triangles.Add(c);
			triangles.Add(a);
		}

		[BurstCompile]
		private static void AddPentagon(int a, int b, int c, int d, int e, ref DynamicBuffer<IntBuffer> triangles)
		{
			triangles.Add(c);
			triangles.Add(b);
			triangles.Add(a);

			triangles.Add(d);
			triangles.Add(c);
			triangles.Add(a);

			triangles.Add(e);
			triangles.Add(d);
			triangles.Add(a);
		}

		#endregion
		protected override void OnUpdate()
		{
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var level = GetSingleton<LevelComponent>();

			var chunks = m_VoxelSystem.Chunks.GetKeyArray(Allocator.Temp);
			foreach (var entry in chunks)
			{
				var voxels = m_VoxelSystem.Chunks[entry];
#error TODODODODO

			}
			Entities
				.WithBurst()
				.WithName("Do_Triangulation")
				.WithAll<RenderMesh>()
				.ForEach((Entity entity, int entityInQueryIndex, ref ChunkComponent cc, in TriangulateTag updateData) =>
				{
					int edgeCacheMin = 0, edgeCacheMax = 0;

					var rowCacheMax = new NativeArray<int>(LevelComponent.VoxelResolution * 2 + 1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					var rowCacheMin = new NativeArray<int>(LevelComponent.VoxelResolution * 2 + 1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

					barrier.RemoveComponent<TriangulateTag>(entityInQueryIndex, entity);

					var vertices = barrier.AddBuffer<VectorBuffer>(entityInQueryIndex, entity);
					var triangles = barrier.AddBuffer<IntBuffer>(entityInQueryIndex, entity);


					FillFirstRowCache(level.chunkSize);
					TriangulateCellRows(level.chunkSize);
					if (cc.upNeighbour != Entity.Null)
					{
						TriangulateGapRow(level.chunkSize);
					}
					rowCacheMax.Dispose();
					rowCacheMin.Dispose();

					barrier.AddComponent<MeshAssignTag>(entityInQueryIndex, entity);


						#region wtf tbh					
						void FillFirstRowCache(float chunkSize)
					{
							// first corner
							var voxels = bfe[entity];
						CacheFirstCorner(voxels[0]);
						int i;
						for (i = 0; i < LevelComponent.VoxelResolution - 1; i++)
						{
							CacheNextEdgeAndCorner(i * 2, voxels[i], voxels[i + 1]);
						}
						if (xNeighbour != Entity.Null)
						{
							var n = bfe[xNeighbour][0].Value.CopyDummyX(chunkSize);
							CacheNextEdgeAndCorner(i * 2, voxels[i], n);
						}
					}


					void CacheFirstCorner(Voxel voxel)
					{
						if (voxel.state)
						{
							rowCacheMax[0] = vertices.Length;
							vertices.Add(voxel.position);
						}
					}

					void CacheNextEdgeAndCorner(int index, Voxel xMin, Voxel xMax)
					{
						Debug.Assert(xMin.position.y == xMax.position.y);
						if (xMin.state != xMax.state)
						{
							rowCacheMax[index + 1] = vertices.Length;
							vertices.Add(xMin.XEdgePoint);
						}
						if (xMax.state)
						{
							rowCacheMax[index + 2] = vertices.Length;
							vertices.Add(xMax.position);
						}
					}

					void TriangulateCellRows(float chunkSize)
					{
						var voxels = bfe[entity];
						int cells = LevelComponent.VoxelResolution - 1;
						for (int i = 0, y = 0; y < cells; y++, i++)
						{
							SwapRowCaches();

							CacheFirstCorner(voxels[i + LevelComponent.VoxelResolution]);
							CacheNextMiddleEdge(voxels[i], voxels[i + LevelComponent.VoxelResolution]);
							for (int x = 0; x < cells; x++, i++)
							{
								var a = voxels[i];
								var b = voxels[i + 1];
								var c = voxels[i + LevelComponent.VoxelResolution];
								var d = voxels[i + LevelComponent.VoxelResolution + 1];
								int cacheIndex = x * 2;
								CacheNextEdgeAndCorner(cacheIndex, c, d);
								CacheNextMiddleEdge(b, d);
								TriangulateCell(cacheIndex, a, b, c, d);
							}
							if (xNeighbour != Entity.Null)
							{
								TriangulateGapCell(chunkSize, i);
							}
						}
					}



					void SwapRowCaches()
					{
						var temp = rowCacheMin;
						rowCacheMin = rowCacheMax;
						rowCacheMax = temp;
					}

					void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
					{
						Debug.Assert(yMin.position.x == yMax.position.x);
						Debug.Assert(yMin.position.y < yMax.position.y);
						edgeCacheMin = edgeCacheMax;
						if (yMin.state != yMax.state)
						{
							edgeCacheMax = vertices.Length;
							vertices.Add(yMin.YEdgePoint);
						}
					}

					void TriangulateGapCell(float chunkSize, int i)
					{
						Debug.Assert(i % LevelComponent.VoxelResolution == (LevelComponent.VoxelResolution - 1));
						var voxels = bfe[entity];
						int cacheIndex = (LevelComponent.VoxelResolution - 1) * 2;
						var a = voxels[i];
						var b = bfe[xNeighbour][i + 1 - LevelComponent.VoxelResolution].Value.CopyDummyX(chunkSize);
						var c = voxels[i + LevelComponent.VoxelResolution];
						var d = bfe[xNeighbour][i + 1].Value.CopyDummyX(chunkSize);

						CacheNextEdgeAndCorner(cacheIndex, voxels[i + LevelComponent.VoxelResolution], d);
						CacheNextMiddleEdge(b, d);
						TriangulateCell(cacheIndex, a, b, c, d);
					}

					void TriangulateGapRow(float chunkSize)
					{

						int cells = LevelComponent.VoxelResolution - 1;
						int offset = cells * LevelComponent.VoxelResolution;

						var voxels = bfe[entity];
						var neighbourVoxels = bfe[yNeighbour];

						var dummyY = neighbourVoxels[0].Value.CopyDummyY(chunkSize);

						SwapRowCaches();
						CacheFirstCorner(dummyY);
						CacheNextMiddleEdge(voxels[cells * LevelComponent.VoxelResolution], dummyY);

						for (int x = 0; x < cells; x++)
						{
							var dummyT = dummyY.CopyDummyX(0);
							dummyY = neighbourVoxels[x + 1].Value.CopyDummyY(chunkSize);
							var cacheIndex = x * 2;
							CacheNextEdgeAndCorner(cacheIndex, dummyT, dummyY);
							CacheNextMiddleEdge(voxels[x + offset + 1], dummyY);
							TriangulateCell(cacheIndex, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
						}

						if (xyNeighbour != Entity.Null)
						{
							var leftVoxels = bfe[xNeighbour];
							var a = voxels[voxels.Length - 1];
							var b = leftVoxels[leftVoxels.Length - LevelComponent.VoxelResolution].Value.CopyDummyX(chunkSize);
							var c = neighbourVoxels[LevelComponent.VoxelResolution - 1].Value.CopyDummyY(chunkSize);
							var d = bfe[xyNeighbour][0].Value.CopyDummyXY(chunkSize);
							Debug.Assert(a.Value.position.x == c.position.x);
							Debug.Assert(a.Value.position.y == b.position.y);
							Debug.Assert(b.position.x == d.position.x);
							Debug.Assert(c.position.y == d.position.y);
							var cacheIndex = cells * 2;
							CacheNextEdgeAndCorner(cacheIndex, c, d);
							CacheNextMiddleEdge(b, d);
							TriangulateCell(cacheIndex, a, b, c, d);
						}
					}

					void TriangulateCell(int i, in Voxel a, in Voxel b, in Voxel c, in Voxel d)
					{
						var cas = 0;
						if (a.state) cas |= 0b0001;
						if (b.state) cas |= 0b0010;
						if (c.state) cas |= 0b0100;
						if (d.state) cas |= 0b1000;
						switch (cas)
						{
							case 0:
								break;
							case 1:
								AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1], ref triangles);
								break;
							case 2:
								AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax, ref triangles);
								break;
							case 3:
								AddQuad(rowCacheMin[i], edgeCacheMin, edgeCacheMax, rowCacheMin[i + 2], ref triangles);
								break;
							case 4:
								AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin, ref triangles);
								break;
							case 5:
								AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], rowCacheMin[i + 1], ref triangles);
								break;
							case 6:
								AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax, ref triangles);
								AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin, ref triangles);
								break;
							case 7:
								AddPentagon(
									rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMax, rowCacheMin[i + 2], ref triangles);
								break;
							case 8:
								AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1], ref triangles);
								break;
							case 9:
								AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1], ref triangles);
								AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1], ref triangles);
								break;
							case 10:
								AddQuad(rowCacheMin[i + 1], rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2], ref triangles);
								break;
							case 11:
								AddPentagon(
									rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin, rowCacheMax[i + 1], rowCacheMax[i + 2], ref triangles);
								break;
							case 12:
								AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, ref triangles);
								break;
							case 13:
								AddPentagon(
									rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, rowCacheMin[i + 1], rowCacheMin[i], ref triangles);
								break;
							case 14:
								AddPentagon(
									rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMin, rowCacheMax[i], ref triangles);
								break;
							case 15:
								AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 2], rowCacheMin[i + 2], ref triangles);
								break;
						}
					}

						#endregion


					}).ScheduleParallel();

			m_Barrier.AddJobHandleForProducer(Dependency);

		}
	}

	[UpdateAfter(typeof(TriangulateChunkSystem))]
	public class MeshChunkAssignSystem : JobComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			RequireForUpdate(GetEntityQuery(typeof(MeshAssignTag)));
		}
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps.Complete();
			var barrier = m_Barrier.CreateCommandBuffer();
			Entities
				.WithoutBurst()
				.WithAll<MeshAssignTag>()
				.ForEach((Entity entity, ref ChunkComponent cc, in RenderMesh rnd, in DynamicBuffer<IntBuffer> triangles, in DynamicBuffer<VectorBuffer> vertices) =>
				{
					barrier.RemoveComponent<MeshAssignTag>(entity);
					var trianglesArray = new int[triangles.Length]; ;// triangles.Reinterpret<int>().ToArray();
					var verticesArray = new Vector3[vertices.Length]; // vertices.Reinterpret<Vector3>().ToArray();

					for (int i = 0; i < triangles.Length; i++)
					{
						trianglesArray[i] = triangles[i];
					}
					for (int i = 0; i < vertices.Length; i++)
					{
						verticesArray[i] = vertices[i];
					}
					rnd.mesh.Clear();
					rnd.mesh.vertices = verticesArray;
					rnd.mesh.triangles = trianglesArray;
					rnd.mesh.normals = verticesArray.Select(x => new Vector3(0, 0, 1)).ToArray();
					rnd.mesh.RecalculateBounds();

					barrier.SetSharedComponent(entity, rnd);
				}).Run();

			return inputDeps;
		}
	}
}
