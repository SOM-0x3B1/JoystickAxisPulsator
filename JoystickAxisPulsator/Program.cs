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
        static void Main(string[] args)
        {
            #region welcomePrompt
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("====================================");
            Console.WriteLine("||   Joystick Axis Pulsator v0.1  ||");            
            Console.WriteLine("||        by SOM-0x3B1            ||");
            Console.WriteLine("====================================");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nWarning: ");
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
            #endregion


            // Initialize DirectInput
            DirectInput directInput = new DirectInput();
            Guid joystickGuid = Guid.Empty;

            List<DeviceInstance> gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
            List<DeviceInstance> joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();

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

            #region joystickSelection
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nGamepads:");
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = 0; i < gamepads.Count; i++)
                Console.WriteLine($"  {i + 1}.\t{gamepads[i].ProductName}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nJoysticks:");
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = gamepads.Count; i < joysticks.Count; i++)
                Console.WriteLine($"  {i + 1}.\t{joysticks[i - gamepads.Count].ProductName}");


            while (joystickGuid == Guid.Empty)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Select the number of your gamepad/joystick: ");
                Console.ForegroundColor = ConsoleColor.White;
                int id = int.Parse(Console.ReadLine()) - 1;
                if (id < gamepads.Count)
                    joystickGuid = gamepads[id].ProductGuid;
                else if (id > gamepads.Count && id < joysticks.Count)
                    joystickGuid = joysticks[id].ProductGuid;
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nInvalid id. Please enter a valid number (1-{joysticks.Count + gamepads.Count}).");
                }
            }
            #endregion


            // Instantiate the joystick
            Joystick joystick = new Joystick(directInput, joystickGuid);

            Console.WriteLine($"Joystick/Gamepad GUID: {joystickGuid}");

            // Query all suported ForceFeedback effects
            IList<EffectInfo> allEffects = joystick.GetEffects();
            foreach (EffectInfo effectInfo in allEffects)
                Console.WriteLine("Effect available {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

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