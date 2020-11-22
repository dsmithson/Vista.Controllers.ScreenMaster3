using System;
using System.Collections.Generic;
using System.Text;

namespace Spyder.Controllers.ScreenMaster3
{
	public delegate void KeyboardTBarEventHandler(object sender, KeyboardTBarEventArgs e);

	public class KeyboardTBarEventArgs : EventArgs
	{
		public int TBarPosition { get; set; }

		public KeyboardTBarEventArgs(int tBarPosition)
		{
			this.TBarPosition = tBarPosition;
		}
	}
}
