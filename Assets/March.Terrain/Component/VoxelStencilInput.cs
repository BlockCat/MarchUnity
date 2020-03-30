using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace Mixed
{
	public enum VoxelShape
	{
		Rectangle, Circle
	}
	public struct VoxelStencilInput : IComponentData
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
		}

		public void Deserialize(uint tick, ref DataStreamReader reader, VoxelStencilInput baseline, NetworkCompressionModel compressionModel)
		{
			Deserialize(tick, ref reader);
		}

		public void Serialize(ref DataStreamWriter writer)
		{
			writer.WriteByte(fillType ? (byte)1 : (byte)0);
			writer.WriteFloat(centerX);
			writer.WriteFloat(centerY);
			writer.WriteFloat(radius);
		}

		public void Serialize(ref DataStreamWriter writer, VoxelStencilInput baseline, NetworkCompressionModel compressionModel)
		{
			Serialize(ref writer);
		}

		internal bool InRange(float2 pos)
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

	/*
	public class VoxelStencilSendCommandSystem : CommandSendSystem<VoxelStencilInput> { }
	public class VoxelStencilReceiveCommandSystem : CommandReceiveSystem<VoxelStencilInput> { }
	*/
}
