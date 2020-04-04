using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.March.Player.Authoring
{
	[GenerateAuthoringComponent]
	public struct MovablePlayerComponent : IComponentData
	{
		[GhostDefaultField]
		public int PlayerId;
	}
}
