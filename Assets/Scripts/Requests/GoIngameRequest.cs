using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.NetCode;
using Unity.Networking.Transport;

[BurstCompile]
public struct GoInGameRequest : IRpcCommand
{

	static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
		new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
	
	public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
	{
		return InvokeExecuteFunctionPointer;
	}

	public void Deserialize(ref DataStreamReader reader) { }

	public void Serialize(ref DataStreamWriter writer) { }

	[BurstCompile]
	private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
	{
		RpcExecutor.ExecuteCreateRequestComponent<GoInGameRequest>(ref parameters);
	}
}

public class GoInGameRequestSystem : RpcCommandRequestSystem<GoInGameRequest>
{
}