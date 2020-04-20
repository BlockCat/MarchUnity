using Assets.March.Player;
using Assets.March.Player.Authoring;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;


[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
public class Game : ComponentSystem
{
	struct InitGameComponent : IComponentData { };


	protected override void OnDestroy()
	{
		base.OnDestroy();
	}
	protected override void OnCreate()
	{
		var c = EntityManager.CreateEntity(typeof(InitGameComponent));
#if UNITY_EDITOR
		EntityManager.SetName(c, "InitGameEntity");
#endif

		RequireSingletonForUpdate<InitGameComponent>();
	}
	protected override void OnUpdate()
	{
		Debug.Log("Starting Game");
		EntityManager.DestroyEntity(GetSingletonEntity<InitGameComponent>());

		foreach (var world in World.All)
		{
			Debug.Log("Loading client");
			var network = world.GetExistingSystem<NetworkStreamReceiveSystem>();
			if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
			{
				//var ep = NetworkEndPoint.Parse("2001:1c02:2f1a:c400:90f1:be4a:921f:0567", 7979, NetworkFamily.Ipv6);
				var ep = NetworkEndPoint.LoopbackIpv4;
				ep.Port = 7979;
				network.Connect(ep);
			}


#if UNITY_EDITOR || UNITY_SERVER
			else if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
			{
				Debug.Log("Loading server");
				NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
				ep.Port = 7979;
				network.Listen(ep);
			}
#endif
		}
	}
}


[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
public class GoInGameClientSystem : ComponentSystem
{
	protected override void OnUpdate()
	{
		Entities
			.WithNone<NetworkStreamInGame>()
			.ForEach((Entity entity, ref NetworkIdComponent netId) =>
			{				
				Debug.Log("Try setting up client connection");
				// Add state that the connection entity is in game 
				PostUpdateCommands.AddComponent<NetworkStreamInGame>(entity);

				// Create entity that handles in game request
				var req = PostUpdateCommands.CreateEntity();
				PostUpdateCommands.AddComponent<GoInGameRequest>(req);
				PostUpdateCommands.AddComponent(req, new SendRpcCommandRequestComponent { TargetConnection = entity });
			});

	}
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class GoInGameServerSystem : ComponentSystem
{
	protected override void OnUpdate()
	{
		Entities
			.WithNone<SendRpcCommandRequestComponent>()
			.ForEach((Entity reqEntity, ref GoInGameRequest req, ref ReceiveRpcCommandRequestComponent reqSrc) =>
			{
				Debug.Log("Try responding to client request");
				PostUpdateCommands.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);
				var playerId = EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value;
				UnityEngine.Debug.Log($"Server received a connection and put the id to: {playerId}");

				// Get the collection of ghosts (things that are drawn and predicted on client but where server has authority over)
				var ghostCollection = GetSingleton<GhostPrefabCollectionComponent>();

				// Find the id of the sphere prefab (our player)
				var ghostId = MarchingSquaresGhostSerializerCollection.FindGhostType<SphereSnapshotData>();

				// Get the prefab entity from ... server prefabs?
				var prefab = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostCollection.serverPrefabs)[ghostId].Value;
				var player = EntityManager.Instantiate(prefab);

				EntityManager.SetComponentData(player, new MovablePlayerComponent { PlayerId = playerId });

				// Allow the player to receive and handle inputs
				PostUpdateCommands.AddBuffer<PlayerInput>(player);

				// Add which entity contains the command stream
				PostUpdateCommands.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent { targetEntity = player });

				PostUpdateCommands.DestroyEntity(reqEntity);

			});
	}
}