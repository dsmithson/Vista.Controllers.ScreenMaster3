using System;
using System.Threading.Tasks;

namespace Spyder.Controllers.ScreenMaster3.TestConsole
{
    class Program
    {
        private static int joyX, joyY, joyZ, tBar, lastButtonPressed;
        private static int[] rotaries = new int[10];
        private static bool lastButtonIsPressed;

        private static KeyboardInterface keyboard;

        static async Task Main(string[] args)
        {
            Console.Write("Initializing console connection... ");
            keyboard = new KeyboardInterface();
            if (!await keyboard.StartupAsync())
            {
                Console.WriteLine("Failed.  Exiting...");
                return;
            }
            Console.WriteLine("Success");

            keyboard.TBarValueChanged += Keyboard_TBarValueChanged;
            keyboard.KeyPressed += Keyboard_KeyPressed;
            keyboard.KeyReleased += Keyboard_KeyReleased;
            keyboard.RotaryValueChanged += Keyboard_RotaryValueChanged;
            keyboard.JoystickValueChanged += Keyboard_JoystickValueChanged;
            keyboard.KeyAction += Keyboard_KeyAction;
            Console.WriteLine("Listening for events");

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();

            Console.WriteLine("Shutting down...");
            await keyboard.ShutdownAsync(); 
        }

        private static async Task UpdateStatisticsButtons()
        {
            keyboard.SetLcdButton(0, lastButtonIsPressed ? LcdColor.Red : LcdColor.Green, "Button: " + lastButtonPressed);
            keyboard.SetLcdButton(1, LcdColor.Green, "JoyX:", joyX, "");
            keyboard.SetLcdButton(2, LcdColor.Green, "JoyY:", joyY, "");
            keyboard.SetLcdButton(3, LcdColor.Green, "JoyZ:", joyZ, "");
            keyboard.SetLcdButton(4, LcdColor.Green, "T-Bar:", tBar, "");

            for(int i=0; i<rotaries.Length; i++)
            {
                keyboard.SetLcdButton(8 + i, LcdColor.Green, $"Rotary {i + 1}: {rotaries[i]}");
            }

            await keyboard.UpdateAllDisplaysAsync();
        }

        private static async void Keyboard_KeyAction(object sender, KeyboardKeyEventArgs e)
        {
            lastButtonPressed = e.KeyIndex;
            lastButtonIsPressed = e.IsPressed;
            Console.WriteLine($"Keyboard action - key {e.KeyIndex} pressed = {e.IsPressed}");
            await UpdateStatisticsButtons();
        }

        private static async void Keyboard_JoystickValueChanged(object sender, KeyboardJoystickEventArgs e)
        {
            joyX = e.X;
            joyY = e.Y;
            joyZ = e.Z;
            Console.WriteLine($"Joystick action - X={e.X}\tY={e.Y}\tZ={e.Z}");
            await UpdateStatisticsButtons();
        }

        private static async void Keyboard_RotaryValueChanged(object sender, KeyboardRotaryEventArgs e)
        {
            rotaries[e.RotaryIndex] += e.RotaryOffset;
            Console.WriteLine($"Rotary action - index {e.RotaryIndex} offset by {e.RotaryOffset}");
            await UpdateStatisticsButtons();
        }

        private static void Keyboard_KeyReleased(object sender, KeyboardKeyEventArgs e)
        {
            Console.WriteLine($"Key released - Index {e.KeyIndex}");
        }

        private static void Keyboard_KeyPressed(object sender, KeyboardKeyEventArgs e)
        {
            Console.WriteLine($"Key pressed - Index {e.KeyIndex}");
        }

        private static async void Keyboard_TBarValueChanged(object sender, KeyboardTBarEventArgs e)
        {
            tBar = e.TBarPosition;
            Console.WriteLine($"T-Bar action - Position {e.TBarPosition}");
            await UpdateStatisticsButtons();
        }
    }
}
