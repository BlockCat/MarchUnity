using Unity.Entities;
using Unity.NetCode;
using Unity.Jobs;
using March.Core.Sprite;

namespace Assets.March.Player
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UpdatePlayerSpriteSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
			var tick = group.PredictingTick;
			Entities
				.WithBurst()
				.ForEach((DynamicBuffer<Player.PlayerInput> inputBuffer, ref SpriteInformation sd, ref PredictedGhostComponent prediction) =>
				{
					if (!GhostPredictionSystemGroup.ShouldPredict(tick, prediction))
						return;

					Player.PlayerInput input;
					inputBuffer.GetDataAtTick(tick, out input);

					if (input.Left && input.Right)
					{
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
}
