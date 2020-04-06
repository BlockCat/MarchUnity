using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace March.Terrain
{
	// Request For a load?
	[BurstCompile]
	public struct LevelLoadRequest : IRpcCommand
	{
		public float Size;
		public int ChunkResolution;

		static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer = new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

		public quaternion Rotation { get; internal set; }
		public float3 Position { get; internal set; }

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return InvokeExecuteFunctionPointer;
		}
		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<LevelLoadRequest>(ref parameters);
		}

		public void Deserialize(ref DataStreamReader reader)
		{
			this.Size = reader.ReadFloat();
			this.ChunkResolution = reader.ReadInt();
		}

		public void Serialize(ref DataStreamWriter writer)
		{
			writer.WriteFloat(this.Size);
			writer.WriteInt(this.ChunkResolution);
		}
	}

	public class LevelLoadRequestRpcCommandRequestSystem : RpcCommandRequestSystem<LevelLoadRequest> { }

	[BurstCompile]
	public struct RpcLevelLoaded : IRpcCommand
	{
		private static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer = new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return InvokeExecuteFunctionPointer;
		}

		public void Deserialize(ref DataStreamReader reader)
		{
			
		}

		public void Serialize(ref DataStreamWriter writer)
		{
			
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<LevelLoadRequest>(ref parameters);
		}

	}

	public class LevelLoadedRpcCommandRequestSystem: RpcCommandRequestSystem<RpcLevelLoaded> { }
}
