using Mixed;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
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
		
		foreach (var world in World.All)
		{
			var network = world.GetExistingSystem<NetworkStreamReceiveSystem>();
			if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
			{
				NetworkEndPoint ep = NetworkEndPoint.LoopbackIpv4;
				ep.Port = 7979;
				network.Connect(ep);
			}
#if UNITY_EDITOR
			else
			{
				NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
				ep.Port = 7979;
				network.Listen(ep);
			}
#endif
		}
	}
}
