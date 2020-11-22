using System;
using System.Collections.Generic;
using System.Text;

namespace Spyder.Controllers.ScreenMaster3
{
	/// <summary>
	/// Keyboard message commands
	/// </summary>
	public enum KeyboardCommand : uint
	{
		LoaderMessage = 100,        // arg1=SYSMSG..START,LOAD,REBOOT
		SetLamps = 101,             // cmd only no args
		SetText1 = 102,             // arg1 = board, arg2 = switch
		SetText8 = 103,             // arg1 = board
		SetText48 = 104,            // cmd only no args
		GetStatus = 105,            // no args
		ClearErrors = 106,          // no args, clears all errors
		SetDiagnosticFlags = 107,   // arg1=flags
		TestModules = 108,          // no args
		TestRotaryEncoder = 109,    // no args		
		LoaderStart = 110,          // host Arg2=Address, Arg3=Filesize, Arg4=Filechksum
		LoadBlock = 111,            // host Arg2=BlockID, Arg3=size, Arg4=BlockChkSum
		Reboot = 112                // no args
	}
}
