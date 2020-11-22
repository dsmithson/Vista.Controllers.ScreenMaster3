using System;
using System.Collections.Generic;
using System.Text;

namespace Spyder.Controllers.ScreenMaster3
{
	public delegate void KeyboardRotaryEventHandler(object sender, KeyboardRotaryEventArgs e);

	public class KeyboardRotaryEventArgs : EventArgs
	{
		public int RotaryIndex { get; set; }
		public int RotaryOffset { get; set; }

		public KeyboardRotaryEventArgs(int rotaryIndex, int rotaryOffset)
		{
			this.RotaryIndex = rotaryIndex;
			this.RotaryOffset = rotaryOffset;
		}
	}
}
