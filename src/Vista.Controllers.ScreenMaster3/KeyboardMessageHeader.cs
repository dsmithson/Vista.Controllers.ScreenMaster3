using System;
using System.Collections.Generic;
using System.Text;

namespace Spyder.Controllers.ScreenMaster3
{
	public struct KeyboardMessageHeader
	{
		public const int HEADER_SIZE = 28;

		public char[] Sig;
		public uint Cmd;
		public uint Arg1;
		public uint Arg2;
		public uint Arg3;
		public uint Arg4;

		public KeyboardMessageHeader(KeyboardCommand cmd)
			: this((uint)cmd)
		{
		}

		public KeyboardMessageHeader(uint Cmd)
		{
			Sig = "3216kb".ToCharArray();
			this.Cmd = Cmd;
			this.Arg1 = 0;
			this.Arg2 = 0;
			this.Arg3 = 0;
			this.Arg4 = 0;
		}

		public KeyboardMessageHeader(KeyboardCommand cmd, KeyboardCommand arg1)
			: this((uint)cmd, (uint)arg1)
		{

		}

		public KeyboardMessageHeader(uint Cmd, uint Arg1)
		{
			Sig = "3216kb".ToCharArray();
			this.Cmd = Cmd;
			this.Arg1 = Arg1;
			this.Arg2 = 0;
			this.Arg3 = 0;
			this.Arg4 = 0;
		}

		public KeyboardMessageHeader(uint Cmd, uint Arg1, uint Arg2, uint Arg3, uint Arg4)
		{
			Sig = "3216kb".ToCharArray();
			this.Cmd = Cmd;
			this.Arg1 = Arg1;
			this.Arg2 = Arg2;
			this.Arg3 = Arg3;
			this.Arg4 = Arg4;
		}

		public byte[] GetStream()
		{
			byte[] data = new byte[HEADER_SIZE];
			CopyTo(data);
			return data;
		}

		public int CopyTo(byte[] data, int startIndex = 0)
		{
			CopyArray(data, startIndex + 0, Sig);
			StreamUint(data, startIndex + 8, Cmd);
			StreamUint(data, startIndex + 12, Arg1);
			StreamUint(data, startIndex + 16, Arg2);
			StreamUint(data, startIndex + 20, Arg3);
			StreamUint(data, startIndex + 24, Arg4);

			//Always writes the header size worth of data to the provided stream
			return HEADER_SIZE;
		}

		public void SetHeader(byte[] data)
		{
			Sig = new char[8];
			for (int i = 0; i < 8; i++)
				Sig[i] = (char)data[i];

			Cmd = LoadUint(data, 8);
			Arg1 = LoadUint(data, 12);
			Arg2 = LoadUint(data, 16);
			Arg3 = LoadUint(data, 20);
			Arg4 = LoadUint(data, 24);
		}

		private void CopyArray(byte[] data, int pos, char[] chardata)
		{
			for (int i = 0; i < chardata.Length; i++, pos++)
				data[pos] = (byte)chardata[i];
		}

		private void StreamUint(byte[] data, int pos, uint Value)
		{
			data[pos + 0] = (byte)((Value & 0xff000000) >> 24);
			data[pos + 1] = (byte)((Value & 0x00ff0000) >> 16);
			data[pos + 2] = (byte)((Value & 0x0000ff00) >> 8);
			data[pos + 3] = (byte)(Value & 0x000000ff);
		}

		private uint LoadUint(byte[] data, int pos)
		{
			uint Result = 0;

			Result |= (uint)((uint)data[pos] << 24);
			Result |= (uint)((uint)data[pos + 1] << 16);
			Result |= (uint)((uint)data[pos + 2] << 8);
			Result |= (uint)data[pos + 3];

			return Result;
		}
	}
}
