using Mixed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEditor;

namespace Client
{
	public class LoadLevelSystem : ComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;

		private Material chunkMaterial;

		protected override void OnCreate()
		{
			RequireSingletonForUpdate<LevelLoadRequest>();

			chunkMaterial = Resources.Load<Material>("Chunk");
			if (chunkMaterial == null) throw new Exception("Resource was not found");
			Debug.LogWarning(chunkMaterial.GetType().Name);
		}
		protected override void OnUpdate()
		{
			Debug.Log("Trying to load a level");

			var entity = GetSingletonEntity<LevelLoadRequest>();
			var request = GetSingleton<LevelLoadRequest>();

			EntityManager.DestroyEntity(entity);
			var level = LevelComponent.Create(request.Size, request.ChunkResolution);
			var created = EntityManager.CreateEntity();
			EntityManager.SetName(created, "LevelInformationEntity");
			EntityManager.AddComponentData(created, level);
			EntityManager.AddComponentData(created, new Translation { Value = request.Position });
			EntityManager.AddComponentData(created, new Rotation { Value = request.Rotation });
			EntityManager.AddComponentData(created, new LocalToWorld());

			var random = new Unity.Mathematics.Random();
			random.InitState(1231231453);

			Entity[,] neighbours = new Entity[request.ChunkResolution, request.ChunkResolution];
			for (int y = request.ChunkResolution - 1; y >= 0; y--)
			{
				for (int x = 0; x < request.ChunkResolution; x++)
				{
					var chunkEntity = EntityManager.CreateEntity();
					neighbours[x, y] = chunkEntity;
					EntityManager.SetName(chunkEntity, $"Chunk_{x}_{y}");

					Entity left = x > 0 ? neighbours[x - 1, y] : Entity.Null;
					Entity up = y < request.ChunkResolution - 1 ? neighbours[x, y + 1] : Entity.Null;
					Entity diag = y < request.ChunkResolution - 1 && x > 0 ? neighbours[x - 1, y + 1] : Entity.Null;
					EntityManager.AddComponentData(chunkEntity, new ChunkComponent
					{
						Size = level.chunkSize,
						x = x,
						y = y,
						leftNeighbour = left,
						upNeighbour = up,
						diagNeighbour = diag,
					});
					var mesh = new Mesh();
					mesh.vertices = new Vector3[4]
					{
						new Vector3(0, 0, 0),
						new Vector3(request.Size, 0, 0),
						new Vector3(0, request.Size, 0),
						new Vector3(1, request.Size, 0)
					};
					mesh.triangles = new int[6] { 0, 1, 2, 1, 3, 2 };

					var v = new Vector3(0, 0, 1);
					mesh.normals = new Vector3[4] { v, v, v, v };
					mesh.RecalculateBounds();
					EntityManager.AddComponentData(chunkEntity, new TriangulateTag());
					EntityManager.AddComponentData(chunkEntity, new Parent { Value = created });
					EntityManager.AddComponentData(chunkEntity, new RenderBounds { Value = mesh.bounds.ToAABB() });
					EntityManager.AddComponentData(chunkEntity, new Translation { Value = new float3(x * level.chunkSize, y * level.chunkSize, 0) });
					EntityManager.AddComponentData(chunkEntity, new Rotation { Value = quaternion.Euler(0, 0, 0) });
					EntityManager.AddComponentData(chunkEntity, new LocalToParent());
					EntityManager.AddComponentData(chunkEntity, new LocalToWorld());
					EntityManager.AddSharedComponentData(chunkEntity, new RenderMesh
					{
						mesh = mesh,
						material = chunkMaterial,
						needMotionVectorPass = true,
						layer = 0,
						receiveShadows = false,
						castShadows = UnityEngine.Rendering.ShadowCastingMode.Off,

					});
					var voxelBuffer = EntityManager.AddBuffer<Mixed.Voxel>(chunkEntity);

					for (int vy = 0; vy < LevelComponent.VoxelResolution; vy++)
					{
						for (int vx = 0; vx < LevelComponent.VoxelResolution; vx++)
						{
							bool st = random.NextBool();//(vx + vy) % 2 == 0;
							st = true;
							voxelBuffer.Add(new Mixed.Voxel(st, vx, vy, level.voxelSize));
						}
					}
				}
			}
		}
	}
}
