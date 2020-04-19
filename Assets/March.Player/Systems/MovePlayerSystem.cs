using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Jobs;
using March.Terrain.Authoring;
using March.Core.Sprite;

namespace Assets.March.Player
{

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
				.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<Player.PlayerInput> inputBuffer, ref Translation t, ref SpriteInformation sd, ref PredictedGhostComponent prediction) =>
			{
				if (!GhostPredictionSystemGroup.ShouldPredict(tick, prediction))
					return;

				Player.PlayerInput input;
				inputBuffer.GetDataAtTick(tick, out input);

				if (input.Left)
					t.Value.x += speed * deltaTime;
				if (input.Right)
					t.Value.x -= speed * deltaTime;
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
