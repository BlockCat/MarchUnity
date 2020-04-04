using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Mixed
{
	public struct LevelComponent : IComponentData
	{
		public const int VoxelResolution = 16;
		public float Size, chunkSize, voxelSize, halfSize;
		public int ChunkResolution;

		public static LevelComponent Create(float size, int chunkResolution)
		{
			var chunkSize = size / chunkResolution;
			return new LevelComponent
			{
				Size = size,
				halfSize = size * 0.5f,
				chunkSize = chunkSize,
				voxelSize = chunkSize / VoxelResolution,
				ChunkResolution = chunkResolution,
			};
		}
	}

	public struct ChunkComponent : IComponentData
	{
		public float Size;
		public int x;
		public int y;
		public Entity leftNeighbour;
		public Entity upNeighbour;
		public Entity diagNeighbour;
	}

	[InternalBufferCapacity(LevelComponent.VoxelResolution * LevelComponent.VoxelResolution)]
	public struct Voxel : IBufferElementData
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