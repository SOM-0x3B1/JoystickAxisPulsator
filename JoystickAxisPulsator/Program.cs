using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        private static int frequency = 50;
        private static List<string> alignmentMap = new List<string>();
        private static Coord dotPos = new Coord(0, 0);


        static void DrawTitle()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("====================================");
            Console.WriteLine("||   Joystick Axis Pulsator v0.1  ||");
            Console.WriteLine("||        by SOM-0x3B1            ||");
            Console.WriteLine("====================================\n");
        }

        static void DrawAignmentMap()
        {
            Console.WriteLine("\t\t\t\t\t\t\t\t\t\t\t\t");
            for (int y = 0; y < alignmentMap.Count; y++)
            {
                for (int x = 0; x < alignmentMap[y].Length; x++)
                {
                    if (dotPos.X + 7 == x && dotPos.Y + 2 == y)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.Write("O");
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        if (alignmentMap[y][x] == '#')
                            Console.BackgroundColor = ConsoleColor.White;
                        Console.Write(alignmentMap[y][x]);
                        Console.BackgroundColor = ConsoleColor.Black;
                    }                    
                }
                Console.Write("\t\t\t\t");
                Console.WriteLine();
            }
        }


        static void Main(string[] args)
        {
            using (StreamReader r = new StreamReader("indicator.txt", Encoding.Default))
            {
                while (!r.EndOfStream)
                    alignmentMap.Add(r.ReadLine());
            }

            Console.SetWindowSize(120, 35);

            ShowWarningPrompt();

            gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
            joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();

            if (gamepads.Count > 0)
            {
                joystickGuid = gamepads[0].ProductGuid;
                productName = gamepads[0].ProductName;           
            }
            else if(joysticks.Count > 0)
            {
                joystickGuid = joysticks[0].ProductGuid;
                productName = joysticks[0].ProductName;
            }

            if (joystickGuid != Guid.Empty)
            {
                joystick = new Joystick(directInput, joystickGuid);
                joystick.Properties.BufferSize = 128;
                joystick.Acquire();
            }

            ShowMainMenu();  
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
            Console.WriteLine("\nBy accepting the following prompt, you acknowledge that I (SOM-0x3B1) do not\n" +
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
            Console.Write("  4. Select pulse frequency");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   [{frequency} Hz]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  5. Start pulsing");


            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nWhat would you like to do? (1-5): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while(id < 1 || id > 5)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nInvalid id. Please enter a valid number (1-4).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nWhat would you like to do? (1-5): ");
                Console.ForegroundColor = ConsoleColor.White;

                id = int.Parse(Console.ReadLine());
            }

            switch (id)
            {
                case 1:
                    SelectJoystick();
                    break;
                case 2:
                    CalibrateDevice();
                    break;
                case 4:
                    SelectFrequency();
                    break;
                default:
                    ShowMainMenu(); 
                    break;
            }
        }

        static void SelectJoystick()
        {
            Console.Clear();
            DrawTitle();

            Console.WriteLine("\nSearching for compatible devices...");

            joystickGuid = Guid.Empty;
            gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
            joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();

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

            joystick = new Joystick(directInput, joystickGuid);
            joystick.Properties.BufferSize = 128;
            joystick.Acquire();

            ShowMainMenu();
        }

        static void CalibrateDevice()
        {
            Console.Clear();
            DrawTitle();


            //Dictionary<string, Axis> detectedInputs = new Dictionary<string, Axis>(); 
            List<Axis> allInputs = new List<Axis>();
            Dictionary<string, int> registeredInputs = new Dictionary<string, int>();

            string[] supportedInputs = { "X axis", "Y axis", "Roll axis", "Pause button" };
            int cInputIndex = 0;
            Dictionary<string, Axis> inputs = new Dictionary<string, Axis>();

            string[] calPositions = { "front left", "back right" };
            int cCalPosIndex = 0;

            int calibrationPhase = 0;


            Console.CursorVisible = false;

            // Poll events from joystick
            while (true)
            {
                joystick.Poll();
                JoystickUpdate[] datas = joystick.GetBufferedData();
                foreach (JoystickUpdate state in datas)
                {
                    string inputType = state.Offset.ToString();
                    if (!registeredInputs.ContainsKey(inputType))
                    {
                        registeredInputs.Add(inputType, allInputs.Count);
                        allInputs.Add(new Axis(inputType, state.Value));
                    }
                    else
                        allInputs[registeredInputs[inputType]].cValue = state.Value;
                }

                Console.SetCursorPosition(0, 5);

                switch (calibrationPhase)
                {
                    case 0:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Move your joystick around to detect input types.\n");
                        Console.ForegroundColor = ConsoleColor.White;

                        Console.WriteLine("Inputs detected: \n");
                        for (int i = 0; i < allInputs.Count; i++)
                            Console.WriteLine($"  {i + 1}. {allInputs[i].name} ->  {allInputs[i].cValue}\t\t\t\t\t\t\t");
                        break;
                    case 1:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Set your joystick to its {calPositions[cCalPosIndex]} position, then press enter.");
                        Console.ForegroundColor = ConsoleColor.White;                                                                    
                        break;
                    case 2:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Move your joystick around to test its configuration, then set it to its default (middle) position.");
                        Console.ForegroundColor = ConsoleColor.White;
                        dotPos.X = (int)Math.Round(inputs["X axis"].GetPercent() * 31);
                        dotPos.Y = (int)Math.Round(inputs["Y axis"].GetPercent() * 12);
                        break;
                    case 3:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Move the joystick VERY slightly around to select its deadzone.");
                        Console.ForegroundColor = ConsoleColor.White;

                        foreach (var i in inputs)
                            i.Value.UpdateDeadZone();
                        break;
                }

                Console.WriteLine("\t\t\t\t\t\t\t\t\t\t\t\t");
                foreach (var i in inputs)
                {
                    Console.Write($"{i.Key}: ");
                    Console.Write(i.Value == null ? "ignore" : $"{i.Value.name} -> {i.Value.cValue}");
                    Console.Write("\t\t\t\t\t\t\t\t\t\t\t\t\n");
                }

                switch (calibrationPhase)
                {
                    case 0:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\nPress the number of your {supportedInputs[cInputIndex]} (or press enter to ignore it)  ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 1:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\nPress enter to confirm.");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 2:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\nPress enter to proceed, or 'R' to reconfigure.");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();
                    if (consoleKeyInfo.Key == ConsoleKey.Escape)
                        break;
                    else if ('1' <= consoleKeyInfo.KeyChar && consoleKeyInfo.KeyChar <= allInputs.Count.ToString()[0])
                    {
                        switch (calibrationPhase)
                        {
                            case 0:
                                inputs[supportedInputs[cInputIndex]] = allInputs[int.Parse(consoleKeyInfo.KeyChar.ToString()) - 1];
                                cInputIndex++;
                                break;
                        }
                    }
                    else if (consoleKeyInfo.Key == ConsoleKey.Enter)
                    {
                        switch (calibrationPhase)
                        {
                            case 0:
                                inputs[supportedInputs[cInputIndex]] = null;
                                cInputIndex++;
                                if (cInputIndex == supportedInputs.Length)
                                {
                                    calibrationPhase++;
                                    dotPos.X = 0;
                                    dotPos.Y = 0;
                                }
                                break;
                            case 1:
                                if (cCalPosIndex == 0) {
                                    inputs["X axis"].SetMin();
                                    inputs["Y axis"].SetMin();
                                    dotPos.X = 31;
                                    dotPos.Y = 12;
                                }
                                else
                                {
                                    inputs["X axis"].SetMax();
                                    inputs["Y axis"].SetMax();
                                }
                                cCalPosIndex++;
                                if (cCalPosIndex == calPositions.Length)
                                    calibrationPhase++;
                                break;
                        }
                    }
                }

                Thread.Sleep(1000 / frequency);
            }

            Console.CursorVisible = true;

            ShowMainMenu();
        }

        static void SelectFrequency()
        {
            Console.Clear();
            DrawTitle();

            Console.WriteLine("Select frequency\n");

            Console.WriteLine("  1. 10  Hz \t (low precision; high stability)");
            Console.WriteLine("  2. 20  Hz");
            Console.WriteLine("  3. 50  Hz");
            Console.WriteLine("  4. 100 Hz \t (high precision; low stability)");


            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nChoose your frequency (1-4): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 4)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nInvalid id. Please enter a valid number (1-4).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nChoose your frequency (1-4): ");
                Console.ForegroundColor = ConsoleColor.White;

                id = int.Parse(Console.ReadLine());
            }

            switch (id)
            {
                case 1:
                    frequency = 10;
                    break;
                case 2:
                    frequency = 20;
                    break;
                case 3:
                    frequency = 50;
                    break;
                case 4:
                    frequency = 100;
                    break;
            }

            ShowMainMenu();
        }
    }
}