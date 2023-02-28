using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoystickAxisPulsator
{
    internal class Program
    {
        private static List<DeviceInstance> gamepads;
        private static List<DeviceInstance> joysticks;
        private static DirectInput directInput = new DirectInput();
        private static Guid joystickGuid = Guid.Empty;
        private static Joystick joystick;
        private static string productName = "";

        static void DrawTitle()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("====================================");
            Console.WriteLine("||   Joystick Axis Pulsator v0.1  ||");
            Console.WriteLine("||        by SOM-0x3B1            ||");
            Console.WriteLine("====================================\n");
        }

        static void Main(string[] args)
        {            
            ShowWarningPrompt();

            ShowMainMenu();            
            
            gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
            joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();
            SelectJoystick();

            joystick = new Joystick(directInput, joystickGuid);
            joystick.Properties.BufferSize = 128;
            joystick.Acquire();

            CalibrateDevice();
        }

        static void ShowWarningPrompt()
        {
            DrawTitle();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Warning: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("The program will read the axis values of the selected joystick, \n" +
                "and output an alternating pulse of emulated keypresses to a selected window. \n" +
                "Since the app will spam these presses at a very high rate,");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" they may cause some \n" +
                "unexpected issues with your applications and system.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nBy accepting the following propmt, you acknowledge that I, SOM-0x3B1 do not\n" +
                "take responsibility for (but will gladly help with) any problems that this \n" +
                "software might cause. \n\n" +
                "Use this software at your own risk.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n\nDo you accept these terms? [Y/N]: ");
            Console.ForegroundColor = ConsoleColor.White;

            string input = Console.ReadLine().ToLower();
            while (input != "n" && input != "y")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nInvalid format. Please use 'Y' or 'N'.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Do you accept these terms? [Y/N]: ");
                Console.ForegroundColor = ConsoleColor.White;

                input = Console.ReadLine().ToLower();
            }
            if (input == "n")
                Environment.Exit(0);
        }

        static void ShowMainMenu()
        {
            Console.Clear();
            DrawTitle();

            Console.WriteLine("Main menu\n");

            Console.Write("  1. Select gamepad/joystick");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (joystickGuid == Guid.Empty)
                Console.WriteLine($"  [none]");
            else
                Console.WriteLine($"  [{productName}]");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("  2. Calibrate device");
            Console.WriteLine("  3. Select target window");
            Console.WriteLine("  4. Start pulsing");


            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nWhat would you like to do? (1-4): ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadLine();
        }

        static void SelectJoystick()
        {
            Console.Clear();
            DrawTitle();

            Console.WriteLine("\nSearching for compatible devices...");

            // If Joystick not found, throws an error
            while (gamepads.Count + joysticks.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No gamepad/joystick found.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Press enter to retry.");

                Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Searching for compatible devices...");
                gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
                joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();
            }

            Console.WriteLine("\nDevices found:");

            Console.WriteLine("\n  Gamepads:");
            
            for (int i = 0; i < gamepads.Count; i++)
                Console.WriteLine($"\t{i + 1}. {gamepads[i].ProductName}");
            if (gamepads.Count == 0)
                Console.WriteLine("\t-");

            Console.WriteLine("\n  Joysticks:");
            for (int i = gamepads.Count; i < joysticks.Count; i++)
                Console.WriteLine($"\t{i + 1}. {joysticks[i - gamepads.Count].ProductName}");
            if (joysticks.Count == 0)
                Console.WriteLine("\t-");


            while (joystickGuid == Guid.Empty)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n\nSelect the number of your gamepad/joystick: ");
                Console.ForegroundColor = ConsoleColor.White;
                int id = int.Parse(Console.ReadLine()) - 1;
                if (id < gamepads.Count)
                {
                    joystickGuid = gamepads[id].ProductGuid;
                    productName = gamepads[id].ProductName;
                }
                else if (id >= gamepads.Count && id < joysticks.Count - gamepads.Count)
                {
                    joystickGuid = joysticks[id].ProductGuid;
                    productName = joysticks[id].ProductName;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nInvalid id. Please enter a valid number (1-{joysticks.Count + gamepads.Count}).");
                }
            }
            joysticks.Clear();
            gamepads.Clear();

            ShowMainMenu();
        }

        static void CalibrateDevice()
        {
            Console.Clear();
            DrawTitle();

            // Poll events from joystick
            while (true)
            {
                joystick.Poll();
                JoystickUpdate[] datas = joystick.GetBufferedData();
                foreach (JoystickUpdate state in datas)
                    Console.WriteLine(state);
            }
        }
    }
}