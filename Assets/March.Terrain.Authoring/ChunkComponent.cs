using System;
using Unity.Entities;
using Unity.NetCode;

namespace March.Terrain.Authoring
{
	[GenerateAuthoringComponent]
	public struct ChunkComponent : ISharedComponentData, IEquatable<ChunkComponent>
	{
		//public float Size;
		[GhostDefaultField] public int x;
		[GhostDefaultField] public int y;

		public bool Equals(ChunkComponent other)
		{
			return x == other.x && y == other.y;
		}
	}
}