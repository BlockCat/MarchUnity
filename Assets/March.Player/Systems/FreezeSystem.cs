using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

namespace Assets.March.Player.Systems
{

	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public class FreezeSystem : SystemBase
	{
		private EntityQuery m_Group;
		private EndSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			m_Group = GetEntityQuery(ComponentType.ReadOnly(typeof(RequestFreeze)));
			m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			RequireForUpdate(m_Group);
		}
		protected override void OnUpdate()
		{
			//var barrier = m_Barrier.CreateCommandBuffer().ToConcurrent();
			Entities
				.WithStoreEntityQueryInField(ref m_Group)
				.WithBurst()
				.ForEach((Entity entity, int entityInQueryIndex, ref PhysicsVelocity v, in RequestFreeze _) =>
				{
					v.Linear.z = 0;
					v.Angular = new float3(0);
				})
				.ScheduleParallel();

			//m_Barrier.AddJobHandleForProducer(Dependency);
		}
	}
}
