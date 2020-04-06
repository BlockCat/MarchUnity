using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace March.Terrain.Authoring
{
	public enum VoxelShape
	{
		Rectangle = 0b01, Circle = 0b10
	}

	public struct VoxelStencilInput : IComponentData//ICommandData<VoxelStencilInput>
	{
		public uint Tick { get; set; }
		public bool fillType;
		public float centerX, centerY, radius;
		public VoxelShape shape;

		public float XStart => centerX - radius;
		public float XEnd => centerX + radius;
		public float YStart => centerY - radius;
		public float YEnd => centerY + radius;


		public void Deserialize(uint tick, ref DataStreamReader reader)
		{
			this.Tick = tick;
			this.fillType = reader.ReadByte() == 1;
			this.centerX = reader.ReadFloat();
			this.centerY = reader.ReadFloat();
			this.radius = reader.ReadFloat();
			this.shape = (VoxelShape)reader.ReadInt();
		}

		public void Deserialize(uint tick, ref DataStreamReader reader, VoxelStencilInput baseline, NetworkCompressionModel compressionModel)
		{
			this.Tick = tick;
			this.fillType = reader.ReadByte() == 1;
			this.centerX = reader.ReadPackedFloat(compressionModel);
			this.centerY = reader.ReadPackedFloat(compressionModel);
			this.radius = reader.ReadPackedFloat(compressionModel);
			this.shape = (VoxelShape)reader.ReadPackedInt(compressionModel);
			Deserialize(tick, ref reader);
		}

		public void Serialize(ref DataStreamWriter writer)
		{
			writer.WriteByte(fillType ? (byte)1 : (byte)0);
			writer.WriteFloat(centerX);
			writer.WriteFloat(centerY);
			writer.WriteFloat(radius);
			writer.WriteInt((int)shape);
		}

		public void Serialize(ref DataStreamWriter writer, VoxelStencilInput baseline, NetworkCompressionModel compressionModel)
		{
			writer.WriteByte(fillType ? (byte)1 : (byte)0);
			writer.WritePackedFloat(centerX, compressionModel);
			writer.WritePackedFloat(centerY, compressionModel);
			writer.WritePackedFloat(radius, compressionModel);
			writer.WritePackedInt((int)shape, compressionModel);

		}

		public bool InRange(float2 pos)
		{
			float px = pos.x;
			float py = pos.y;
			switch (shape)
			{
				case VoxelShape.Circle:
					var dx = px - centerX;
					var dy = py - centerY;
					var c = dx * dx + dy * dy <= radius * radius;
					return c;
				//return px * px + py * py <= radius * radius;
				//return pos.x * pos.x + pos.y * pos.y <= radius * radius;
				case VoxelShape.Rectangle:
					return px >= XStart && px <= XEnd && py >= YStart && py <= YEnd;
				//return pos.x >= XEnd && pos.x <= XStart && pos.y >= YStart && pos.y <= YEnd;
				default:
					throw new ArgumentException();
			}
		}
	}


	// public class VoxelStencilSendCommandSystem : CommandSendSystem<VoxelStencilInput> { }
	// public class VoxelStencilReceiveCommandSystem : CommandReceiveSystem<VoxelStencilInput> { }

}
