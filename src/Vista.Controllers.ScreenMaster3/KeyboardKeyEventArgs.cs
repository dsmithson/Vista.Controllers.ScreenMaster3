using System;

namespace Spyder.Controllers.ScreenMaster3
{
	public delegate void KeyboardKeyEventHandler(object sender, KeyboardKeyEventArgs e);

	public class KeyboardKeyEventArgs : EventArgs
	{
		public int KeyIndex { get; set; }
		public bool IsPressed { get; set; }

		public KeyboardKeyEventArgs(int keyIndex, bool isPressed)
		{
			this.KeyIndex = keyIndex;
			this.IsPressed = isPressed;
		}
	}
}
