using System;
using System.Collections.Generic;
using System.Text;

namespace Spyder.Controllers.ScreenMaster3
{
	public enum KeyboardEventType : uint
	{
		KeyPress = 0,  //arg1=ID, arg2=action
		Joystick = 1,  //arg1=X,arg2=Y,arg3=Z
		TBar = 2,
		RotaryEncoder = 3,
		System = 4,
		Error = 5       //Loader error
	}
}
