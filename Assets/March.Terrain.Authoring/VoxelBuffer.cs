using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;

namespace March.Terrain.Authoring
{
	
	[InternalBufferCapacity(LevelComponent.VoxelResolution * LevelComponent.VoxelResolution)]
	[GenerateAuthoringComponent]
	public struct VoxelBuffer : IBufferElementData
	{
		public Voxel Value;

		public static implicit operator Voxel(VoxelBuffer e) => e.Value;
		public static implicit operator VoxelBuffer(Voxel e) => new VoxelBuffer { Value = e };
	}
}
