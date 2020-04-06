using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace March.Terrain.Authoring
{
	public struct LevelComponent : IComponentData
	{
		public const int VoxelResolution = 16;
		public float Size, chunkSize, voxelSize, halfSize;
		public int ChunkResolution;

		public static LevelComponent Create(float size, int chunkResolution)
		{
			var chunkSize = size / chunkResolution;
			return new LevelComponent
			{
				Size = size,
				halfSize = size * 0.5f,
				chunkSize = chunkSize,
				voxelSize = chunkSize / VoxelResolution,
				ChunkResolution = chunkResolution,
			};
		}
	}
}