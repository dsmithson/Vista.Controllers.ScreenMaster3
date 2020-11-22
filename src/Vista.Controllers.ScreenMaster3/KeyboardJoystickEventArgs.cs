using System;
using System.Collections.Generic;
using System.Text;

namespace Spyder.Controllers.ScreenMaster3
{
	public delegate void KeyboardJoystickEventHandler(object sender, KeyboardJoystickEventArgs e);

	public class KeyboardJoystickEventArgs : EventArgs
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Z { get; set; }

		public KeyboardJoystickEventArgs(int x, int y, int z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}
	}
}
