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
		public uint Tick { get; private set; }
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
			switch(shape)
			{
				case VoxelShape.Circle:
					return pos.x * pos.x + pos.y * pos.y <= radius * radius;
				case VoxelShape.Rectangle:
					return pos.x >= XEnd && pos.x <= XStart && pos.y >= YStart && pos.y <= YEnd;
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
