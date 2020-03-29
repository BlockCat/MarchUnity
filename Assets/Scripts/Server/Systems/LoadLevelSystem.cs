using Mixed;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Server
{

	/*public class LoadLevelSystem : JobComponentSystem
	{
		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery m_LevelGroup;

		protected override void OnCreate()
		{
			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_LevelGroup = GetEntityQuery(ComponentType.ReadWrite<LevelComponent>());
			RequireSingletonForUpdate<ServerSettings>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (m_LevelGroup.IsEmptyIgnoreFilter)
			{
				var settings = GetSingleton<ServerSettings>();
				var level = EntityManager.CreateEntity();
				EntityManager.AddComponentData(level, LevelComponent.Create(settings.Size, settings.ChunkResolution));

				return inputDeps;
			}
			else
			{
				JobHandle levelDep;
				var commandBuffer = m_Barrier.CreateCommandBuffer();
				var level = m_LevelGroup.ToComponentDataArrayAsync<LevelComponent>(Allocator.TempJob, out levelDep);
				var handle = Entities
					.WithBurst()
					.WithDeallocateOnJobCompletion(level)
					.ForEach((Entity entity, int entityInQueryIndex) =>
					{
						var req = commandBuffer.CreateEntity();
						commandBuffer.AddComponent(req, new LevelLoadRequest
						{
							Size = level[0].Size,
							ChunkResolution = level[0].ChunkResolution
						});
					})
					.Schedule(JobHandle.CombineDependencies(inputDeps, levelDep));
				m_Barrier.AddJobHandleForProducer(handle);
				return handle;
			}
		}
	}*/
}