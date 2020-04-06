using Unity.Entities;
using Unity.Mathematics;

namespace March.Terrain.Authoring
{

	public struct Voxel
	{
		public bool state {
			get => typeState == 1;
			set => typeState = value ? (byte)1 : (byte)0;
		}
		public float2 XEdgePoint => new float2(xEdge, position.y);
		public float2 YEdgePoint => new float2(position.x, yEdge);

		public byte typeState;
		public float2 position;
		public float xEdge, yEdge;
		public float2 xNormal, yNormal;

		public Voxel(bool state, int x, int y, float size)
		{
			typeState = state ? (byte)1 : (byte)0;
			position.x = (x + 0.5f) * size;
			position.y = (y + 0.5f) * size;

			//xEdge = position.x + 0.5f * size;
			yEdge = 0;// float.MinValue;
			xEdge = position.x + 0.5f * size;// float.MinValue;
			yEdge = position.y + 0.5f * size;

			xNormal = float2.zero;
			yNormal = float2.zero;
		}

		public void SetState(bool state)
		{
			this.state = state;
		}

		public Voxel Copy(bool newState)
		{
			return new Voxel
			{
				typeState = newState ? (byte)1 : (byte)0,
				position = new float2(position.x, position.y),
				xEdge = xEdge,
				yEdge = yEdge,
				xNormal = xNormal,
				yNormal = yNormal
			};
		}
		public Voxel CopyDummyX(float chunkSize)
		{
			return new Voxel
			{
				typeState = this.typeState,
				position = new float2(position.x + chunkSize, position.y),
				xEdge = xEdge + chunkSize,
				yEdge = yEdge,
				xNormal = xNormal,
				yNormal = yNormal
			};
		}

		public Voxel CopyDummyY(float chunkSize)
		{
			return new Voxel
			{
				typeState = this.typeState,
				position = new float2(position.x, position.y + chunkSize),
				xEdge = xEdge,
				yEdge = yEdge + chunkSize,
				xNormal = xNormal,
				yNormal = yNormal
			};
		}

		public Voxel CopyDummyXY(float chunkSize)
		{
			return new Voxel
			{
				typeState = this.typeState,
				position = new float2(position.x + chunkSize, position.y + chunkSize),
				xEdge = xEdge + chunkSize,
				yEdge = yEdge + chunkSize,
				xNormal = xNormal,
				yNormal = yNormal
			};
		}
	}


}