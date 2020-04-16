using Assets.March.Player.Authoring.Gravity;
using March.Player.Authoring;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Assets.March.Player
{
	public static class Gravity
	{
		[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
		[UpdateAfter(typeof(UpdateStateSystem))]
		public class GravitySystem : SystemBase
		{
			EntityQuery m_Group;
			protected override void OnCreate()
			{
				m_Group = GetEntityQuery(typeof(GravityComponent), typeof(CurrentVelocityComponent));
				RequireForUpdate(m_Group);
			}
			protected override void OnUpdate()
			{
				CompleteDependency();
				var deltaTime = Time.DeltaTime;
				Entities
					.WithStoreEntityQueryInField(ref m_Group)
					.WithNone<OnGroundState>()
					.ForEach((ref CurrentVelocityComponent velocity, in GravityComponent gravity) =>
					{
						var y = velocity.Velocity.y;
						if (y > 0)
						{
							velocity.Velocity.y -= gravity.AccelerationUp * deltaTime;
						}
						else
						{
							velocity.Velocity.y -= gravity.AccelerationDown * deltaTime;
						}
					}).ScheduleParallel();

				Entities
					.WithStoreEntityQueryInField(ref m_Group)
					.WithAll<OnGroundState, GravityComponent>()
					.ForEach((ref CurrentVelocityComponent velocity) =>
					{
						velocity.Velocity.y = 0;
					}).ScheduleParallel();
			}
		}

		[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
		public class UpdateStateSystem : SystemBase
		{
			private BeginSimulationEntityCommandBufferSystem m_Barrier;
			private EntityQuery m_Group;

			protected override void OnCreate()
			{
				m_Group = GetEntityQuery(typeof(GravityState));
				m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			}
			protected override void OnUpdate()
			{
				var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();

				var groundData = GetComponentDataFromEntity<OnGroundState>(true);
				var prejumpData = GetComponentDataFromEntity<PreJumpState>(true);
				var postjumpData = GetComponentDataFromEntity<PostJumpState>(true);
				var jumpData = GetComponentDataFromEntity<JumpState>(true);

				Entities
					.WithName("update_gravity_state")
					.WithReadOnly(groundData)
					.WithReadOnly(prejumpData)
					.WithReadOnly(jumpData)
					.WithReadOnly(postjumpData)
					.WithStoreEntityQueryInField(ref m_Group)
					.WithBurst()
					.ForEach((Entity entity, int entityInQueryIndex, in GravityState gState) =>
					{
						switch (gState.state)
						{
							case GravityState.State.OnGround:
								if (!groundData.HasComponent(entity))
									barrier.AddComponent<OnGroundState>(entityInQueryIndex, entity);
								if (prejumpData.HasComponent(entity))
									barrier.RemoveComponent<PreJumpState>(entityInQueryIndex, entity);
								if (postjumpData.HasComponent(entity))
									barrier.RemoveComponent<PostJumpState>(entityInQueryIndex, entity);
								if (jumpData.HasComponent(entity))
									barrier.RemoveComponent<JumpState>(entityInQueryIndex, entity);
								break;
							case GravityState.State.Jump:
								if (groundData.HasComponent(entity))
									barrier.RemoveComponent<OnGroundState>(entityInQueryIndex, entity);
								if (prejumpData.HasComponent(entity))
									barrier.RemoveComponent<PreJumpState>(entityInQueryIndex, entity);
								if (postjumpData.HasComponent(entity))
									barrier.RemoveComponent<PostJumpState>(entityInQueryIndex, entity);
								if (!jumpData.HasComponent(entity))
									barrier.AddComponent<JumpState>(entityInQueryIndex, entity);
								break;
							case GravityState.State.PreJump:
								if (groundData.HasComponent(entity))
									barrier.RemoveComponent<OnGroundState>(entityInQueryIndex, entity);
								if (!prejumpData.HasComponent(entity))
									barrier.AddComponent<PreJumpState>(entityInQueryIndex, entity);
								if (postjumpData.HasComponent(entity))
									barrier.RemoveComponent<PostJumpState>(entityInQueryIndex, entity);
								if (jumpData.HasComponent(entity))
									barrier.RemoveComponent<JumpState>(entityInQueryIndex, entity);
								break;
							case GravityState.State.PostJump:
								if (groundData.HasComponent(entity))
									barrier.RemoveComponent<OnGroundState>(entityInQueryIndex, entity);
								if (prejumpData.HasComponent(entity))
									barrier.RemoveComponent<PreJumpState>(entityInQueryIndex, entity);
								if (!postjumpData.HasComponent(entity))
									barrier.AddComponent<PostJumpState>(entityInQueryIndex, entity);
								if (jumpData.HasComponent(entity))
									barrier.RemoveComponent<JumpState>(entityInQueryIndex, entity);
								break;
						}
					}).ScheduleParallel();

				m_Barrier.AddJobHandleForProducer(this.Dependency);
			}
		}

		/*[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
		[UpdateBefore(typeof(UpdateStateSystem))]
		public class GroundCheckSystem : SystemBase
		{
			private int m_DefaultLayer;
			private int m_PlayerLAyer;
			private int m_LevelLayer;
			private CollisionFilter m_filter;
			private EndSimulationEntityCommandBufferSystem m_Barrier;
			private ExportPhysicsWorld m_exportPhysicsWorld;
			private BuildPhysicsWorld m_buildPhysicsWorld;

			protected override void OnCreate()
			{
				m_DefaultLayer = LayerMask.NameToLayer("Default");
				m_PlayerLAyer = LayerMask.NameToLayer("Player");
				m_LevelLayer = LayerMask.NameToLayer("Level");

				var mask = (uint)1 << m_DefaultLayer | (uint)1 << m_PlayerLAyer | (uint)1 << m_LevelLayer;


				m_filter = new CollisionFilter
				{
					BelongsTo = 0xffffffff,
					CollidesWith = 0xffffffff,// (uint)LayerMask.NameToLayer("Level"),
					GroupIndex = 0
				};
				m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
				m_buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
				m_exportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
			}
			protected override void OnUpdate()
			{
				m_exportPhysicsWorld.FinalJobHandle.Complete();

				var barrier = m_Barrier.CreateCommandBuffer();
				var physicsWorld = m_buildPhysicsWorld.PhysicsWorld;
				var filter = m_filter;
				var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
				var deltaTime = Time.DeltaTime;

				Entities
					.WithName("ground_check")
					.WithoutBurst()
					.ForEach((Entity entity, ref GravityState state, ref PredictedGhostComponent predictionData, in CurrentVelocityComponent vel, in Translation translation) =>
					{
						if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictionData))
							return;
						if (vel.Velocity.y > 0)
							return;

						CompleteDependency();


						var colliWorld = physicsWorld.CollisionWorld;
						var startOffset = -0.4f;
						var distance = (-vel.Velocity.y * deltaTime) + 0.5f;

						var origin = new float3(translation.Value);
						origin.y += startOffset;
						var castInput = new RaycastInput
						{
							Start = origin,
							End = origin - new float3(0, distance, 0),
							Filter = filter
						};

						Debug.DrawLine(origin, origin - new float3(0, distance, 0));

						var closestHit = new Unity.Physics.RaycastHit();						

						if (colliWorld.CastRay(castInput, out closestHit))
						{
							state.state = GravityState.State.OnGround;
							if (HasComponent<OnGroundState>(entity))
							{
								barrier.SetComponent(entity, new OnGroundState
								{
									Y = closestHit.Position.y
								});
							}
							else
							{
								barrier.AddComponent(entity, new OnGroundState
								{
									Y = closestHit.Position.y
								});
							}
						}
						else
						{
							state.state = GravityState.State.Jump;						
						}
					}).Run();

				Entities
					.WithName("gravity_ground_fix")
					.WithBurst()
					.ForEach((ref Translation t, ref OnGroundState s) =>
					{
						t.Value.y = s.Y +0.5f;
					}).Run();

			}
		}
		*/


	}
}
