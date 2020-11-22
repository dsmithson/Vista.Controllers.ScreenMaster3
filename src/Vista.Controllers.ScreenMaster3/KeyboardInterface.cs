using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Knightware.Threading.Tasks;
using Knightware.Diagnostics;

namespace Spyder.Controllers.ScreenMaster3
{
	public class KeyboardInterface
	{
		private const int PushButtonByteCount = 36; //Lamp values are stored bit-wise - 288 max buttons / 8 bits per byte = 36 bytes
		private const int LcdButtonStartIndex = 288;	//First button press index corresponding to LCD button index 0

		// General purpose statics
		public const int Version = 1;
		public const string KeyboardAddress = "192.168.1.3";
		public const int KeyboardPort = 9995;

		public const int PushButtonCount = PushButtonByteCount * 8;
		public const int LcdButtonCount = 48;
		public const int LcdButtonTextLineCount = 3;
		public const int LcdButtonTextCharsPerLine = 6;
		public const int LcdButtonTextMaxLength = LcdButtonTextLineCount * LcdButtonTextCharsPerLine;

		private UdpClient client;
		private IPEndPoint keyboardEndPoint;

		private readonly byte[] lamps;
		private readonly LcdColor[] lcdButtonColors;
		private readonly string[] lcdButtonText;

		/// <summary>
		/// Processes the incoming UDP packets from the console keyboard
		/// </summary>
		private AsyncListProcessor<byte[]> keyboardIncommingCommandProcessor;

		/// <summary>
		/// Processes outgoing UDP commands to the console keyboard
		/// </summary>
		private AsyncListProcessor<(byte[], TaskCompletionSource<bool>)> keyboardOutgoingCommandProcessor;

		public event KeyboardKeyEventHandler KeyPressed;
		public event KeyboardKeyEventHandler KeyReleased;
		public event KeyboardKeyEventHandler KeyAction;
		public event KeyboardJoystickEventHandler JoystickValueChanged;
		public event KeyboardTBarEventHandler TBarValueChanged;
		public event KeyboardRotaryEventHandler RotaryValueChanged;

		public bool IsRunning { get; private set; }

		public KeyboardInterface()
		{
			lamps = new byte[PushButtonByteCount];
			lcdButtonColors = new LcdColor[LcdButtonCount];
			lcdButtonText = new string[LcdButtonCount];
		}

		public async Task<bool> StartupAsync(string keyboardAddress = KeyboardAddress, int keyboardPort = KeyboardPort)
		{
			await ShutdownAsync();

			try
			{
				IsRunning = true;

				keyboardIncommingCommandProcessor = new AsyncListProcessor<byte[]>(ProcessIncomingUdpMessage, () => IsRunning);
				if (!await keyboardIncommingCommandProcessor.StartupAsync())
				{
					await ShutdownAsync();
					return false;
				}

				keyboardOutgoingCommandProcessor = new AsyncListProcessor<(byte[], TaskCompletionSource<bool>)>(ProcessOutgoingUdpCommand, () => IsRunning);
				if (!await keyboardOutgoingCommandProcessor.StartupAsync())
				{
					await ShutdownAsync();
					return false;
				}

				keyboardEndPoint = new IPEndPoint(IPAddress.Parse(keyboardAddress), keyboardPort);
				client = new UdpClient(keyboardPort);
				client.BeginReceive(OnUdpDataReceived, client);

				ClearAllPushButtonsAndLedButtons();
				ClearAllLcdDisplayText();
				Task t1 = UpdateLampsAsync();
				Task t2 = UpdateAllDisplaysAsync();
				await Task.WhenAll(t1, t2);

				return true;
			}
			catch
			{
				await ShutdownAsync();
				return false;
			}
		}

		public async Task ShutdownAsync()
		{
			IsRunning = false;

			if (keyboardIncommingCommandProcessor != null)
			{
				await keyboardIncommingCommandProcessor.ShutdownAsync();
				keyboardIncommingCommandProcessor = null;
			}

			if (keyboardOutgoingCommandProcessor != null)
			{
				await keyboardOutgoingCommandProcessor.ShutdownAsync();
				keyboardOutgoingCommandProcessor = null;
			}

			if (client != null)
			{
				client.Close();
				client.Dispose();
				client = null;
			}

			ClearAllPushButtonsAndLedButtons();
		}

		#region Buttons

		#region Set Push Button Lamps On/Off (Logic)

		public void SetPushButtonLamp(int id, bool on)
		{
			//Sanity check.  If user is trying to set on/off for an LCD button index, map to red/green instead
			if (id >= LcdButtonStartIndex)
			{
				int lcdIndex = GetLcdButtonIndexFromKeyboardIndex(id);
				SetLcdButton(lcdIndex, on ? LcdColor.Red : LcdColor.Green);
			}
			else
			{
				//Update corresponding bit index for lamp
				byte index = (byte)(id / 8);
				byte bit = (byte)(id % 8);
				if (on)
					lamps[index] |= (byte)(1 << bit);
				else
					lamps[index] &= (byte)(~((byte)(1 << bit)));
			}
		}

		#endregion

		#region Set LCD Display Buttons (Logic)

		public void SetLcdButton(int id, LcdColor color)
		{
			lcdButtonColors[id] = color;
		}

		public void SetLcdButton(int id, string text)
		{
			lcdButtonText[id] = text;
		}

		public void SetLcdButton(int id, LcdColor color, string text)
        {
			SetLcdButton(id, text);
			SetLcdButton(id, color);
        }

		public void SetLcdButton(int id, LcdColor color, object line1, object line2, object line3)
        {
			//Build text string (3 lines)
			StringBuilder builder = new StringBuilder(LcdButtonTextMaxLength);
			builder.Append((line1 ?? "").ToString().Substring(0, LcdButtonTextCharsPerLine).PadRight(LcdButtonTextCharsPerLine));
			builder.Append((line2 ?? "").ToString().Substring(0, LcdButtonTextCharsPerLine).PadRight(LcdButtonTextCharsPerLine));
			builder.Append((line3 ?? "").ToString().Substring(0, LcdButtonTextCharsPerLine).PadRight(LcdButtonTextCharsPerLine));

			SetLcdButton(id, color, builder.ToString());
		}

		public void SetAllLcdButtons(LcdColor color = LcdColor.Off)
		{
			for (int i = 0; i < LcdButtonCount; i++)
				lcdButtonColors[i] = color;
		}

		#endregion

		public void ClearAllPushButtonsAndLedButtons()
		{
			Array.Clear(lamps, 0, lamps.Length);
			Array.Clear(lcdButtonColors, 0, lcdButtonColors.Length);
			ClearAllLcdDisplayText();
		}

		public void ClearAllLcdDisplayText()
		{
			for (int i = 0; i < LcdButtonCount; i++)
				lcdButtonText[i] = string.Empty;
		}

		#region Push Data to keyboard

		public Task<bool> UpdateLampsAsync()
		{
			KeyboardMessageHeader header = new KeyboardMessageHeader(KeyboardCommand.SetLamps);

			int dataSize = KeyboardMessageHeader.HEADER_SIZE + lamps.Length + LcdButtonCount;
			byte[] data = new byte[dataSize];

			header.CopyTo(data);
			Array.Copy(lamps, 0, data, KeyboardMessageHeader.HEADER_SIZE, lamps.Length);
			Array.Copy(lcdButtonColors, 0, data, KeyboardMessageHeader.HEADER_SIZE + lcdButtonColors.Length, LcdButtonCount);

			return SendData(data);
		}

		public Task<bool> UpdateAllDisplaysAsync()
		{
			KeyboardMessageHeader header = new KeyboardMessageHeader(KeyboardCommand.SetText48);

			int dataSize = KeyboardMessageHeader.HEADER_SIZE + (LcdButtonCount * LcdButtonTextMaxLength);
			byte[] data = new byte[dataSize];

			header.CopyTo(data);
			WriteTextDataBuffer(0, LcdButtonCount, data);

			return SendData(data);
		}

		public Task<bool> UpdateOneDisplayAsync(int buttonIndex)
		{
			KeyboardMessageHeader header = new KeyboardMessageHeader(KeyboardCommand.SetText1);

			//This is just a guess - I'm sure the button ID needs to be set, but I haven't tested this next line
			header.Arg1 = (uint)buttonIndex;

			byte[] data = new byte[KeyboardMessageHeader.HEADER_SIZE + LcdButtonTextMaxLength];
			header.CopyTo(data);
			WriteTextDataBuffer(buttonIndex, 1, data);

			return SendData(data);
		}

		public Task<bool> UpdateEightDisplaysAsync(int startIndex)
		{
			KeyboardMessageHeader header = new KeyboardMessageHeader(KeyboardCommand.SetText8);
			byte[] data = new byte[KeyboardMessageHeader.HEADER_SIZE + (LcdButtonTextMaxLength * 8)];

			header.CopyTo(data);
			WriteTextDataBuffer(startIndex, 8, data);

			return SendData(data);
		}

		protected void WriteTextDataBuffer(int startIndex, int buttonCount, byte[] buffer, int bufferStartPos = KeyboardMessageHeader.HEADER_SIZE)
        {
			for (int i = 0; i < buttonCount; i++)
			{
				string text = lcdButtonText[startIndex + i];
				if (!string.IsNullOrWhiteSpace(text))
				{
					int dstStart = ((startIndex + i) * LcdButtonTextMaxLength) + bufferStartPos;
					Array.Copy(text.ToCharArray(), 0, buffer, dstStart, Math.Min(text.Length, LcdButtonTextMaxLength));
				}
			}
		}

		public Task ResetKeyboardAsync()
		{
			KeyboardMessageHeader header = new KeyboardMessageHeader(KeyboardCommand.LoaderMessage, KeyboardCommand.Reboot);
			byte[] data = new byte[KeyboardMessageHeader.HEADER_SIZE];
			header.CopyTo(data);

			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			keyboardOutgoingCommandProcessor.Add((data, tcs));
			return tcs.Task;
		}

		private async Task ProcessOutgoingUdpCommand(AsyncListProcessorItemEventArgs<(byte[], TaskCompletionSource<bool>)> item)
		{
			byte[] data = item.Item.Item1;
			TaskCompletionSource<bool> tcs = item.Item.Item2;

			try
			{
				int count = await client.SendAsync(data, data.Length, keyboardEndPoint);
				tcs.TrySetResult(count > 0);
			}
			catch (Exception ex)
			{
				TraceQueue.Trace(this, TracingLevel.Warning, $"{ex.GetType().Name} occurred while processing outgoing command: {ex.Message}");
				tcs.TrySetResult(false);
			}
		}

		#endregion

		#endregion

		#region Incoming Data from Keyboard

		private void OnUdpDataReceived(IAsyncResult ar)
		{
			if (!IsRunning)
				return;

			try
			{
				IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = client.EndReceive(ar, ref remoteEP);
				if (data?.Length >= KeyboardMessageHeader.HEADER_SIZE)
				{
					//enqueue data for processing
					keyboardIncommingCommandProcessor.Add(data);
				}
				else
				{
					TraceQueue.Trace(this, TracingLevel.Warning, "Unexpected keyboard message received.  Ignoring...");
				}
			}
			catch (Exception ex)
			{
				TraceQueue.Trace(this, TracingLevel.Warning, $"{ex.GetType().Name} occurred while processing incoming console packet: {ex.Message}");
			}
			finally
			{
				//Start listening for next packet
				if (IsRunning)
					client.BeginReceive(OnUdpDataReceived, client);

			}
		}

		private Task ProcessIncomingUdpMessage(AsyncListProcessorItemEventArgs<byte[]> item)
		{
			byte[] data = item.Item;

			if (data.Length >= KeyboardMessageHeader.HEADER_SIZE)
			{
				KeyboardMessageHeader header = new KeyboardMessageHeader((uint)0);
				header.SetHeader(data);

				if (header.Cmd == (uint)KeyboardEventType.KeyPress)
				{
					int keyIndex = (int)header.Arg1;
					bool isPressed;
					if (header.Arg2 == 1)
					{
						isPressed = true;
						var args = new KeyboardKeyEventArgs(keyIndex, isPressed);
						KeyPressed?.Invoke(this, args);
						KeyAction?.Invoke(this, args);
					}
					else if (header.Arg2 == 2)
					{
						isPressed = false;
						var args = new KeyboardKeyEventArgs(keyIndex, isPressed);
						KeyReleased?.Invoke(this, args);
						KeyAction?.Invoke(this, args);
					}
				}
				else if (header.Cmd == (uint)KeyboardEventType.Joystick)
				{
					JoystickValueChanged?.Invoke(this, new KeyboardJoystickEventArgs((int)header.Arg1, (int)header.Arg2, (int)header.Arg3));
				}
				else if (header.Cmd == (uint)KeyboardEventType.TBar)
				{
					TBarValueChanged?.Invoke(this, new KeyboardTBarEventArgs((short)header.Arg1));
				}
				else if (header.Cmd == (uint)KeyboardEventType.RotaryEncoder)
				{
					RotaryValueChanged?.Invoke(this, new KeyboardRotaryEventArgs((int)header.Arg1, (int)header.Arg2));
				}
			}

			return Task.FromResult(true);
		}

        #endregion

        #region Diagnostics

        public Task<bool> ClearErrorsAsync()
		{
			KeyboardMessageHeader header = new KeyboardMessageHeader(KeyboardCommand.ClearErrors);
			byte[] data = new byte[KeyboardMessageHeader.HEADER_SIZE];
			header.CopyTo(data);
			return SendData(data);
		}

		public Task<bool> UpdateDiagnosticFlagsAsync(byte flags = 0x00)
		{
			KeyboardMessageHeader header = new KeyboardMessageHeader(KeyboardCommand.SetDiagnosticFlags);
			header.Arg1 = flags;

			byte[] data = new byte[KeyboardMessageHeader.HEADER_SIZE];
			header.CopyTo(data);
			return SendData(data);
		}

		#endregion

		#region Static Helpers

		/// <summary>
		/// Button press events from LCD buttons start at index 288, however setting LCD buttons can be done as a zero-based index.  This method translates
		/// a button index to an LCD index
		/// </summary>
		/// <param name="buttonIndex">Button index (typically from a keypress event)</param>
		/// <returns></returns>
		public int GetLcdButtonIndexFromKeyboardIndex(int buttonIndex)
        {
			//Sanity check
			if (buttonIndex < LcdButtonStartIndex)
				throw new ArgumentException($"LCD button press indexes start at {LcdButtonStartIndex}", nameof(buttonIndex));

			return buttonIndex - LcdButtonStartIndex;
		}

		#endregion

		protected Task<bool> SendData(byte[] bytes)
        {
			var tcs = new TaskCompletionSource<bool>();
			keyboardOutgoingCommandProcessor.Add((bytes, tcs));
			return tcs.Task;
        }
	}
}
