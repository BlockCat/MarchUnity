using Assets.March.Player.Authoring;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets.March.Player
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class SamplePlayerInput : ComponentSystem
	{
		protected override void OnCreate()
		{
			RequireSingletonForUpdate<NetworkIdComponent>();
			RequireSingletonForUpdate<EnableMarchingSquaresGhostReceiveSystemComponent>();
		}
		protected override void OnUpdate()
		{
			var localInput = GetSingleton<CommandTargetComponent>().targetEntity;
			// Debug.Log("");
			if (localInput == Entity.Null)
			{
				var localPlayerId = GetSingleton<NetworkIdComponent>().Value;
				Entities
					.WithNone<Player.PlayerInput>()
					.ForEach((Entity entity, ref MovablePlayerComponent comp) =>
					{
						if (comp.PlayerId == localPlayerId)
						{
							PostUpdateCommands.AddBuffer<Player.PlayerInput>(entity);
							PostUpdateCommands.SetComponent(GetSingletonEntity<CommandTargetComponent>(), new CommandTargetComponent { targetEntity = entity });
						}
					});
				return;
			}


			var input = default(Player.PlayerInput);
			input.tick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;


			var up = UnityEngine.Input.GetKey(KeyCode.W);
			var left = UnityEngine.Input.GetKey(KeyCode.A);
			var down = UnityEngine.Input.GetKey(KeyCode.S);
			var right = UnityEngine.Input.GetKey(KeyCode.D);
			var space = UnityEngine.Input.GetKey(KeyCode.Space);

			input.Up = up;
			input.Left = left;
			input.Right = right;
			input.Down = down;
			input.Shoot = space;


			var inputBuffer = EntityManager.GetBuffer<Player.PlayerInput>(localInput);
			inputBuffer.AddCommandData(input);
		}
	}
}
