using Unity.Entities;
using Unity.NetCode;

namespace March.Terrain.Authoring
{
	[GenerateAuthoringComponent]
	public struct ChunkComponent : IComponentData
	{
		public float Size;
		[GhostDefaultField] public int x;
		[GhostDefaultField] public int y;
		public Entity leftNeighbour;
		public Entity upNeighbour;
		public Entity diagNeighbour;
	}
}