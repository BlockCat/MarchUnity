using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.March.Core
{
	public static class InputHelper
	{
		public enum InputType
		{
			Left = 1 << 0,
			Right = 1 << 1,
			Up = 1 << 2,
			Down = 1 << 3,
			Jump = 1 << 4,
			Shoot = 1 << 5,
			SwitchWeapons = 1 << 6
		}

		public static bool GetButton(uint data, InputType type) => (data & (uint)type) > 0;
		public static uint DisableButton(uint data, InputType type) => data & ~((uint)type);
		public static uint EnableButton(uint data, InputType type) => data | (uint)type;
		public static uint SetButton(uint data, InputType type, bool value) => value ? EnableButton(data, type) : DisableButton(data, type);
	}
}
