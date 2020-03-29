using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct ServerSettings : IComponentData
{
	public float Size;
	public int ChunkResolution;
	public int VoxelResolution;
}

