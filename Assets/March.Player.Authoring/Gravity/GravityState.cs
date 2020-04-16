using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.March.Player.Authoring.Gravity
{
	[GenerateAuthoringComponent]
	public struct GravityState : IComponentData
	{
		public enum State
		{
			OnGround = 0b0001,
			PreJump = 0b0010,
			Jump = 0b0100,
			PostJump = 0b1000,
		}

		[GhostDefaultField]
		public State state;
	}

	public struct OnGroundState : IComponentData
	{
		public float Y;
	}
	public struct PreJumpState : IComponentData { }
	public struct JumpState : IComponentData { }
	public struct PostJumpState : IComponentData { }
}
