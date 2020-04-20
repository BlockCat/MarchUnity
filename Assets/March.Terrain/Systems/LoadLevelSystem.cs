
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
using Unity.NetCode;
using March.Terrain.Authoring;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace March.Terrain
{

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class LoadLevelSystem : ComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;

		private Material chunkMaterial;
		private Entity chunkPrefabEntity;

		protected override void OnCreate()
		{
			RequireSingletonForUpdate<LevelLoadRequest>();

			chunkMaterial = Resources.Load<Material>("Chunk");
			if (chunkMaterial == null) throw new Exception("Resource was not found");
		}


		protected override void OnUpdate()
		{
			Debug.Log("Trying to load a level");

			var entity = GetSingletonEntity<LevelLoadRequest>();
			var request = GetSingleton<LevelLoadRequest>();

			EntityManager.DestroyEntity(entity);
			var level = LevelComponent.Create(request.Size, request.ChunkResolution);
			var created = EntityManager.CreateEntity();
#if UNITY_EDITOR
			EntityManager.SetName(created, "LevelInformationEntity");
#endif
			EntityManager.AddComponentData(created, level);
			EntityManager.AddComponentData(created, new Translation { Value = request.Position });
			EntityManager.AddComponentData(created, new Rotation { Value = request.Rotation });
			EntityManager.AddComponentData(created, new LocalToWorld());

			var ghostCollection = GetSingleton<GhostPrefabCollectionComponent>();

			// Find the id of the sphere prefab (our player)
			var ghostId = MarchingSquaresGhostSerializerCollection.FindGhostType<VoxelGridSnapshotData>();

			// Get the prefab entity from ... server prefabs?
			chunkPrefabEntity = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostCollection.serverPrefabs)[ghostId].Value;

			var random = new Unity.Mathematics.Random();
			random.InitState(1231231453);


			for (int y = request.ChunkResolution - 1; y >= 0; y--)
			{
				for (int x = request.ChunkResolution - 1; x >= 0; x--)
				{
					var chunkData = new ChunkComponent
					{
						x = x,
						y = y
					};

					/*var mesh = new Mesh
					{
						vertices = new Vector3[4]
						{
							new Vector3(0, 0, 0),
							new Vector3(request.Size, 0, 0),
							new Vector3(0, request.Size, 0),
							new Vector3(1, request.Size, 0)
						},
						triangles = new int[6] { 0, 1, 2, 1, 3, 2 }
					};

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

					});*/
					float chunkOffsetX = x * level.chunkSize;
					float chunkOffsetY = y * level.chunkSize;
					for (int vy = 0, i = 0; vy < LevelComponent.VoxelResolution; vy++)
					{
						for (int vx = 0; vx < LevelComponent.VoxelResolution; vx++, i++)
						{
							// Temp because... prefab
							var voxelEntity = EntityManager.CreateEntity();

							EntityManager.AddSharedComponentData(voxelEntity, chunkData);
							EntityManager.AddComponentData(voxelEntity, new Translation
							{
								Value = new float3(chunkOffsetX + (vx + 0.5f) * level.voxelSize,chunkOffsetY + (vy + 0.5f) * level.voxelSize,0)
							});
							EntityManager.AddComponentData(voxelEntity, new Voxel(i, true, level.voxelSize));
						}
					}
				}
			}
		}
	}
}
