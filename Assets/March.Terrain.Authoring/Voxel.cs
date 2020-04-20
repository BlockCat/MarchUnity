using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace March.Terrain.Authoring
{

	[Serializable]
	public struct Voxel : IComponentData
	{
		public bool state {
			get => typeState == 1;
			set => typeState = value ? (byte)1 : (byte)0;
		}

		public int index;

		public byte typeState;
		public float xEdge, yEdge;
		public float2 xNormal, yNormal;

		public Voxel(int index, bool state, float size)
		{
			this.index = index;
			this.typeState = state ? (byte)1 : (byte)0;

			xEdge = float.MinValue;
			yEdge = float.MinValue;

			xNormal = float2.zero;
			yNormal = float2.zero;
		}

		public float2 XEdgePoint(in Translation position) => new float2(xEdge, position.Value.y);
		public float2 YEdgePoint(in Translation position) => new float2(position.Value.x, yEdge);

		public void SetState(bool state)
		{
			this.state = state;
		}		
	}


}