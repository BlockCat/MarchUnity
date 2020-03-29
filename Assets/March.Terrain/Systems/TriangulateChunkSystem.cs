using Mixed;

using System.Linq;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Assets.March.Terrain.Systems
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

	[UpdateAfter(typeof(UpdateChunkSystem))]
	public class TriangulateChunkSystem : JobComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			RequireForUpdate(GetEntityQuery(typeof(TriangulateTag)));
			RequireSingletonForUpdate<LevelComponent>();
		}


		#region triangulation

		private static void FillFirstRowCache(Entity entity, float chunkSize, ref ChunkComponent cc, ref BufferFromEntity<Mixed.Voxel> bfe, ref DynamicBuffer<IntBuffer> triangles, ref DynamicBuffer<VectorBuffer> vertices, ref NativeArray<int> rowCacheMax)
		{
			// first corner
			var voxels = bfe[entity];
			CacheFirstCorner(voxels[0], ref vertices, ref rowCacheMax);
			int i;
			for (i = 0; i < LevelComponent.VoxelResolution - 1; i++)
			{
				CacheNextEdgeAndCorner(i * 2, voxels[i], voxels[i + 1], ref vertices, ref rowCacheMax);
			}
			if (cc.leftNeighbour != Entity.Null)
			{
				var n = bfe[cc.leftNeighbour][0].CopyDummyX(chunkSize);
				CacheNextEdgeAndCorner(i * 2, voxels[i], n, ref vertices, ref rowCacheMax);
			}
		}

		[BurstCompile]
		private static void CacheNextEdgeAndCorner(int index, Voxel xMin, Voxel xMax, ref DynamicBuffer<VectorBuffer> vertices, ref NativeArray<int> rowCacheMax)
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

		[BurstCompile]
		private static void CacheFirstCorner(Voxel voxel, ref DynamicBuffer<VectorBuffer> vertices, ref NativeArray<int> rowCacheMax)
		{
			if (voxel.state)
			{
				rowCacheMax[0] = vertices.Length;
				vertices.Add(voxel.position);
			}
		}

		[BurstCompile]
		private static void CacheNextMiddleEdge(Voxel yMin, Voxel yMax, ref int edgeCacheMin, ref int edgeCacheMax, ref DynamicBuffer<VectorBuffer> vertices)
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

		[BurstCompile]
		private static void TriangulateCellRows(Entity entity, float chunkSize, ref ChunkComponent cc, ref BufferFromEntity<Mixed.Voxel> bfe, ref DynamicBuffer<IntBuffer> triangles, ref int edgeCacheMin, ref int edgeCacheMax, ref DynamicBuffer<VectorBuffer> vertices, ref NativeArray<int> rowCacheMax, ref NativeArray<int> rowCacheMin)
		{
			var voxels = bfe[entity];
			int cells = LevelComponent.VoxelResolution - 1;
			for (int i = 0, y = 0; y < cells; y++, i++)
			{
				SwapRowCaches(ref rowCacheMin, ref rowCacheMax);

				CacheFirstCorner(voxels[i + LevelComponent.VoxelResolution], ref vertices, ref rowCacheMax);
				CacheNextMiddleEdge(voxels[i], voxels[i + LevelComponent.VoxelResolution], ref edgeCacheMin, ref edgeCacheMax, ref vertices);
				for (int x = 0; x < cells; x++, i++)
				{
					var a = voxels[i];
					var b = voxels[i + 1];
					var c = voxels[i + LevelComponent.VoxelResolution];
					var d = voxels[i + LevelComponent.VoxelResolution + 1];
					int cacheIndex = x * 2;
					CacheNextEdgeAndCorner(cacheIndex, c, d, ref vertices, ref rowCacheMax);
					CacheNextMiddleEdge(b, d, ref edgeCacheMin, ref edgeCacheMax, ref vertices);
					TriangulateCell(cacheIndex, a, b, c, d, ref triangles, ref vertices, ref edgeCacheMin, ref edgeCacheMax, ref rowCacheMax, ref rowCacheMin);
				}
				if (cc.leftNeighbour != Entity.Null)
				{
					TriangulateGapCell(entity, chunkSize, i, ref cc, ref bfe, ref triangles, ref vertices, ref edgeCacheMin, ref edgeCacheMax, ref rowCacheMax, ref rowCacheMin);
				}
			}
		}

		[BurstCompile]
		private static void TriangulateGapCell(Entity entity, float chunkSize, int i, ref ChunkComponent cc, ref BufferFromEntity<Mixed.Voxel> bfe, ref DynamicBuffer<IntBuffer> triangles, ref DynamicBuffer<VectorBuffer> vertices, ref int edgeCacheMin, ref int edgeCacheMax, ref NativeArray<int> rowCacheMax, ref NativeArray<int> rowCacheMin)
		{
			Debug.Assert(i % LevelComponent.VoxelResolution == (LevelComponent.VoxelResolution - 1));
			var voxels = bfe[entity];
			int cacheIndex = (LevelComponent.VoxelResolution - 1) * 2;
			var dummyX = bfe[cc.leftNeighbour][i + 1].CopyDummyX(chunkSize);
			var dummyT = bfe[cc.leftNeighbour][i + 1 - LevelComponent.VoxelResolution].CopyDummyX(chunkSize);

			CacheNextEdgeAndCorner(cacheIndex, voxels[i + LevelComponent.VoxelResolution], dummyX, ref vertices, ref rowCacheMax);
			CacheNextMiddleEdge(dummyT, dummyX, ref edgeCacheMin, ref edgeCacheMax, ref vertices);
			TriangulateCell(cacheIndex, voxels[i], dummyT, voxels[i + LevelComponent.VoxelResolution], dummyX, ref triangles, ref vertices, ref edgeCacheMin, ref edgeCacheMax, ref rowCacheMax, ref rowCacheMin);
		}

		[BurstCompile]
		private static void TriangulateGapRow(Entity entity, float chunkSize, ref ChunkComponent cc, ref BufferFromEntity<Mixed.Voxel> bfe, ref DynamicBuffer<IntBuffer> triangles, ref DynamicBuffer<VectorBuffer> vertices, ref int edgeCacheMin, ref int edgeCacheMax, ref NativeArray<int> rowCacheMax, ref NativeArray<int> rowCacheMin)
		{

			int cells = LevelComponent.VoxelResolution - 1;
			int offset = cells * LevelComponent.VoxelResolution;

			var voxels = bfe[entity];
			var neighbourVoxels = bfe[cc.upNeighbour];

			var dummyY = neighbourVoxels[0].CopyDummyY(chunkSize);

			SwapRowCaches(ref rowCacheMin, ref rowCacheMax);
			CacheFirstCorner(dummyY, ref vertices, ref rowCacheMax);
			CacheNextMiddleEdge(voxels[cells * LevelComponent.VoxelResolution], dummyY, ref edgeCacheMin, ref edgeCacheMax, ref vertices);

			for (int x = 0; x < cells; x++)
			{
				var dummyT = dummyY.CopyDummyX(0);
				dummyY = neighbourVoxels[x + 1].CopyDummyY(chunkSize);
				var cacheIndex = x * 2;
				CacheNextEdgeAndCorner(cacheIndex, dummyT, dummyY, ref vertices, ref rowCacheMax);
				CacheNextMiddleEdge(voxels[x + offset + 1], dummyY, ref edgeCacheMin, ref edgeCacheMax, ref vertices);
				TriangulateCell(cacheIndex, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY, ref triangles, ref vertices, ref edgeCacheMin, ref edgeCacheMax, ref rowCacheMax, ref rowCacheMin);
			}

			if (cc.diagNeighbour != Entity.Null)
			{
				var leftVoxels = bfe[cc.leftNeighbour];
				var a = voxels[voxels.Length - 1];
				var b = leftVoxels[leftVoxels.Length - LevelComponent.VoxelResolution].CopyDummyX(chunkSize);
				var c = neighbourVoxels[LevelComponent.VoxelResolution - 1].CopyDummyY(chunkSize);
				var d = bfe[cc.diagNeighbour][0].CopyDummyXY(chunkSize);
				Debug.Assert(a.position.x == c.position.x);
				Debug.Assert(a.position.y == b.position.y);
				Debug.Assert(b.position.x == d.position.x);
				Debug.Assert(c.position.y == d.position.y);
				var cacheIndex = cells * 2;
				CacheNextEdgeAndCorner(cacheIndex, c, d, ref vertices, ref rowCacheMax);
				CacheNextMiddleEdge(b, d, ref edgeCacheMin, ref edgeCacheMax, ref vertices);
				TriangulateCell(cacheIndex, a, b, c, d, ref triangles, ref vertices, ref edgeCacheMin, ref edgeCacheMax, ref rowCacheMax, ref rowCacheMin);
			}
		}

		[BurstCompile]
		private static void TriangulateCell(int i, in Voxel a, in Voxel b, in Voxel c, in Voxel d, ref DynamicBuffer<IntBuffer> triangles, ref DynamicBuffer<VectorBuffer> vertices, ref int edgeCacheMin, ref int edgeCacheMax, ref NativeArray<int> rowCacheMax, ref NativeArray<int> rowCacheMin)
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

		[BurstCompile]
		private static void SwapRowCaches(ref NativeArray<int> cacheMin, ref NativeArray<int> cacheMax)
		{
			var temp = cacheMin;
			cacheMin = cacheMax;
			cacheMax = temp;
		}

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
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps.Complete();
			Debug.Log("Triangulating start");
			var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			var bfe = m_Barrier.GetBufferFromEntity<Mixed.Voxel>(true);
			var level = GetSingleton<LevelComponent>();
			var handle = Entities
				.WithBurst()
				.WithReadOnly(bfe)
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


					FillFirstRowCache(entity, level.chunkSize, ref cc, ref bfe, ref triangles, ref vertices, ref rowCacheMax);
					TriangulateCellRows(entity, level.chunkSize, ref cc, ref bfe, ref triangles, ref edgeCacheMin, ref edgeCacheMax, ref vertices, ref rowCacheMax, ref rowCacheMin);
					if (cc.upNeighbour != Entity.Null)
					{
						TriangulateGapRow(entity, level.chunkSize, ref cc, ref bfe, ref triangles, ref vertices, ref edgeCacheMin, ref edgeCacheMax, ref rowCacheMax, ref rowCacheMin);
					}
					rowCacheMax.Dispose();
					rowCacheMin.Dispose();

					barrier.AddComponent<MeshAssignTag>(entityInQueryIndex, entity);
				}).Schedule(inputDeps);

			m_Barrier.AddJobHandleForProducer(handle);

			return handle;
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
