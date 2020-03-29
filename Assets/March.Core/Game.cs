using Mixed;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
public class Game : ComponentSystem
{
	struct InitGameComponent : IComponentData { };

	protected override void OnCreate()
	{		
		var c = EntityManager.CreateEntity(typeof(InitGameComponent));
		EntityManager.SetName(c, "InitGameEntity");
		RequireSingletonForUpdate<InitGameComponent>();
	}
	protected override void OnUpdate()
	{
		Debug.Log("Starting Game");
		EntityManager.DestroyEntity(GetSingletonEntity<InitGameComponent>());
		/*
		var entity = EntityManager.CreateEntity();
		EntityManager.SetName(entity, "LevelLoadRequest");
		EntityManager.AddComponentData(entity, new LevelLoadRequest
		{
			ChunkResolution = 4,
			Size = 4
		});*/
	}
}
