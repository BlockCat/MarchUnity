using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace March.Terrain.Authoring
{
	/*public struct UpdateChunkTag : IComponentData
	{
		public VoxelStencilInput input;
	}*/

	public struct MeshAssignTag : IComponentData { }
	public struct TriangulateTag : IComponentData { }
}
