using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace March.Core.Sprite
{	

	public struct SpriteInformation : IComponentData
	{
		public enum Direction { LEFT = 1, RIGHT = -1 }
		public Direction direction;
		public int animation;
	}	
	
	[Serializable]
	public struct FrameData : IBufferElementData
	{
		public int offset;
		public int count;
	}
}
