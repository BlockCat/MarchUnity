using Unity.Entities;
using UnityEngine;

namespace March.Terrain
{
	[DisallowMultipleComponent]
	[RequiresEntityConversion]
	public class LevelSettings : MonoBehaviour, IConvertGameObjectToEntity
	{

		public float Size = 4;
		public int ChunkResolution = 4;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new LevelLoadRequest
			{
				Size = Size,
				ChunkResolution = ChunkResolution,
				Position = transform.position,
				Rotation = transform.rotation,
			});
		}

		private void OnDrawGizmos()
		{

			Gizmos.DrawWireCube(transform.position + new Vector3(Size / 2, Size / 2, 0), new Vector3(Size, Size, 0));
		}
	}
}