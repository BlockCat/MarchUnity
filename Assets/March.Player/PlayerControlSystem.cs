using Mixed;
using Unity.Entities;

using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Assets.March.Player
{
	public struct PlayerTag : IComponentData { };

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	class PlayerControlSystem : ComponentSystem
	{

		protected override void OnCreate()
		{
			RequireForUpdate(GetEntityQuery(typeof(PlayerTag)));
		}
		protected override void OnUpdate()
		{
			var up = Input.GetKey(KeyCode.W);
			var left = Input.GetKey(KeyCode.A);
			var down = Input.GetKey(KeyCode.S);
			var right = Input.GetKey(KeyCode.D);
			var space = Input.GetKey(KeyCode.Space);
			var time = UnityEngine.Time.deltaTime;
			Debug.Assert(time > 0);
			Entities
				.WithAll<PlayerTag>()
				.ForEach((Entity entity, ref Translation t, ref Rotation r) =>
				{

					t.Value.x += left ? 4 * time : 0;
					t.Value.x -= right ? 4 * time : 0;
					t.Value.y += up ? 4 * time : 0;
					t.Value.y -= down ? 4 * time : 0;

					r.Value = math.mul(quaternion.AxisAngle(new float3(0, 1, 0), time), r.Value);
					if (space)
					{
						Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(0, 1, 0), Color.red);
						Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(0, -1, 0), Color.red);
						Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(1, 0, 0), Color.red);
						Debug.DrawLine(t.Value, (Vector3)t.Value + new Vector3(-1, 0, 0), Color.red);
						var stencilEntity = EntityManager.CreateEntity();
						EntityManager.AddComponentData(stencilEntity, new VoxelStencilInput
						{
							centerX = t.Value.x,
							centerY = t.Value.y,
							fillType = false,
							radius = 1,
							shape = VoxelShape.Circle
						});
					}
				});

		}
	}
}
