using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace March.Player.Authoring
{
	[GhostDefaultComponent(GhostDefaultComponentAttribute.Type.All)]
	[GenerateAuthoringComponent]
	public struct CurrentVelocityComponent : IComponentData
	{
		[GhostDefaultField(4)]
		public float2 Velocity;
	}
}
