using Assets.March.Player.Authoring;
using Assets.March.Core;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using March.Terrain.Authoring;
using March.Core.Sprite;

namespace Assets.March.Player
{
	public static class Player
	{
		public struct Input : ICommandData<Input>
		{
			public uint Tick => tick;
			public uint tick;

			private uint buttons;

			public bool Shoot {
				get => InputHelper.GetButton(buttons, InputHelper.InputType.Shoot);
				set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Shoot, value);
			}
			public bool Up {
				get => InputHelper.GetButton(buttons, InputHelper.InputType.Up);
				set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Up, value);
			}
			public bool Down {
				get => InputHelper.GetButton(buttons, InputHelper.InputType.Down);
				set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Down, value);
			}
			public bool Left {
				get => InputHelper.GetButton(buttons, InputHelper.InputType.Left);
				set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Left, value);
			}
			public bool Right {
				get => InputHelper.GetButton(buttons, InputHelper.InputType.Right);
				set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Right, value);
			}

			public override string ToString()
			{
				return $"[s:{Shoot}, u:{Up}, d:{Down}, l:{Left}, r:{Right} ({buttons})]";
			}

			public void Deserialize(uint tick, ref DataStreamReader reader)
			{
				this.tick = tick;
				buttons = reader.ReadUInt();
			}

			public void Deserialize(uint tick, ref DataStreamReader reader, Input baseline, NetworkCompressionModel compressionModel)
			{
				Deserialize(tick, ref reader);
			}

			public void Serialize(ref DataStreamWriter writer)
			{
				writer.WriteUInt(buttons);
			}

			public void Serialize(ref DataStreamWriter writer, Input baseline, NetworkCompressionModel compressionModel)
			{
				Serialize(ref writer);
			}
		}

		public class PlayerInputSendCommandSystem : CommandSendSystem<Player.Input> { }
		public class PlayerInputReceiveCommandSystem : CommandReceiveSystem<Player.Input> { }

		[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
		public class SampleInput : ComponentSystem
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
						.WithNone<Player.Input>()
						.ForEach((Entity entity, ref MovablePlayerComponent comp) =>
						{
							if (comp.PlayerId == localPlayerId)
							{
								PostUpdateCommands.AddBuffer<Player.Input>(entity);
								PostUpdateCommands.SetComponent(GetSingletonEntity<CommandTargetComponent>(), new CommandTargetComponent { targetEntity = entity });
							}
						});
					return;
				}


				var input = default(Player.Input);
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


				var inputBuffer = EntityManager.GetBuffer<Player.Input>(localInput);
				inputBuffer.AddCommandData(input);
			}
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class UpdatePlayerSpriteSystem : SystemBase
		{
			protected override void OnUpdate()
			{
				var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
				var tick = group.PredictingTick;
				Entities
					.WithBurst()
					.ForEach((DynamicBuffer<Player.Input> inputBuffer, ref SpriteInformation sd, ref PredictedGhostComponent prediction) =>
					{
						if (!GhostPredictionSystemGroup.ShouldPredict(tick, prediction))
							return;

						Player.Input input;
						inputBuffer.GetDataAtTick(tick, out input);

						if (input.Left && input.Right) {
							sd.animation = 0;

						}
						else if (input.Left)
						{
							sd.direction = SpriteInformation.Direction.LEFT;
							sd.animation = 1;
						}
						else if (input.Right)
						{
							sd.direction = SpriteInformation.Direction.RIGHT;
							sd.animation = 1;
						}
						else
						{
							sd.animation = 0;
						}

						if (input.Down && input.Up)
							sd.animation = 0;
						else if (input.Down || input.Up)
							sd.animation = 1;

					}).ScheduleParallel();
			}
		}

		[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
		public class MovePlayerSystem : SystemBase
		{
			private BeginSimulationEntityCommandBufferSystem m_Barrier;

			protected override void OnCreate()
			{
				m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
				RequireForUpdate(GetEntityQuery(typeof(PlayerTag)));
			}
			protected override void OnUpdate()
			{
				var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
				var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
				var tick = group.PredictingTick;
				var deltaTime = Time.DeltaTime;

				var speed = 2f;

				Entities
					.WithBurst()
					.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<Player.Input> inputBuffer, ref Translation t, ref SpriteInformation sd, ref PredictedGhostComponent prediction) =>
				{
					if (!GhostPredictionSystemGroup.ShouldPredict(tick, prediction))
						return;

					Player.Input input;
					inputBuffer.GetDataAtTick(tick, out input);

					if (input.Left)
					{
						t.Value.x += speed * deltaTime;
					}

					if (input.Right)
					{
						t.Value.x -= speed * deltaTime;
					}

					if (input.Up)
						t.Value.y += speed * deltaTime;
					if (input.Down)
						t.Value.y -= speed * deltaTime;

					if (input.Shoot)
					{
						/*Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(0, 1, 0), Color.red);
						Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(0, -1, 0), Color.red);
						Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(1, 0, 0), Color.red);
						Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(-1, 0, 0), Color.red);*/

						var stencilEntity = barrier.CreateEntity(entityInQueryIndex);
						barrier.AddComponent(entityInQueryIndex, stencilEntity, new VoxelStencilInput
						{
							centerX = t.Value.x,
							centerY = t.Value.y,
							fillType = false,
							radius = 1f,
							shape = VoxelShape.Circle
						});
					}
				}).ScheduleParallel();
				m_Barrier.AddJobHandleForProducer(Dependency);

			}
		}
	}
}
