using Unity.Entities;
using Unity.NetCode;

namespace March.Player.Authoring
{
	[GhostDefaultComponent(GhostDefaultComponentAttribute.Type.All)]
	[GenerateAuthoringComponent]
	public struct GravityComponent : IComponentData
	{
		[GhostDefaultField(3)]
		public float AccelerationUp;
		[GhostDefaultField(3)]
		public float AccelerationDown;
	}
}
