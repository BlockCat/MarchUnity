using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace Mixed
{
	public struct UpdateChunkTag : IComponentData
	{
		public VoxelStencilInput input;
	}

	public struct TriangulateTag : IComponentData { }
	public struct MeshAssignTag : IComponentData { }
}
