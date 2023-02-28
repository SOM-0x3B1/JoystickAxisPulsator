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
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Welcome to Joystick Axis Pulsator v0.1");

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nDisclaimer: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("The program will read the values of the selected joystick, \n" +
                "and output an alternating pulse of keypresses to the selected window. \n" +
                "These emulated keypresses may cause some unexpected issues with your programs \n" +
                "and system. By accepting the following propmt, you acknowledge that \n" +
                "I (the developer of this tool) do not take responsibility for any of these issues. \n" +
                "Use this software at your own risk.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nDo you accept these terms? [Y/N]: ");
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

            // If Joystick not found, throws an error
            if (gamepads.Count + joysticks.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No gamepad/joystick found.");
                Console.ReadKey();
                Environment.Exit(1);
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
            var joystick = new Joystick(directInput, joystickGuid);

            Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);

            // Query all suported ForceFeedback effects
            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
                Console.WriteLine("Effect available {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

            // Poll events from joystick
            while (true)
            {
                joystick.Poll();
                var datas = joystick.GetBufferedData();
                foreach (var state in datas)
                    Console.WriteLine(state);
            }
        }
    }
}