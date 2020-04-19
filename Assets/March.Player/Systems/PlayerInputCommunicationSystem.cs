using Assets.March.Core;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Assets.March.Player
{
	public struct PlayerInput : ICommandData<PlayerInput>
	{
		public uint Tick => tick;
		public uint tick;

		private uint buttons;

		public bool Shoot {
			get => InputHelper.GetButton(buttons, InputHelper.InputType.Shoot);
			set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Shoot, value);
		}
		public bool Up {
			get => InputHelper.GetButton(buttons, InputHelper.InputType.Up);
			set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Up, value);
		}
		public bool Down {
			get => InputHelper.GetButton(buttons, InputHelper.InputType.Down);
			set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Down, value);
		}
		public bool Left {
			get => InputHelper.GetButton(buttons, InputHelper.InputType.Left);
			set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Left, value);
		}
		public bool Right {
			get => InputHelper.GetButton(buttons, InputHelper.InputType.Right);
			set => buttons = InputHelper.SetButton(buttons, InputHelper.InputType.Right, value);
		}

		public override string ToString()
		{
			return $"[s:{Shoot}, u:{Up}, d:{Down}, l:{Left}, r:{Right} state: ({buttons})]";
		}

		public void Deserialize(uint tick, ref DataStreamReader reader)
		{
			this.tick = tick;
			buttons = reader.ReadUInt();
		}

		public void Deserialize(uint tick, ref DataStreamReader reader, PlayerInput baseline, NetworkCompressionModel compressionModel)
		{
			Deserialize(tick, ref reader);
		}

		public void Serialize(ref DataStreamWriter writer)
		{
			writer.WriteUInt(buttons);
		}

		public void Serialize(ref DataStreamWriter writer, PlayerInput baseline, NetworkCompressionModel compressionModel)
		{
			Serialize(ref writer);
		}
	}

	public class PlayerInputSendCommandSystem : CommandSendSystem<PlayerInput> { }
	public class PlayerInputReceiveCommandSystem : CommandReceiveSystem<PlayerInput> { }
}

