﻿using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput.Native;
using WindowsInput;

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
        private static int frequency = 20;
        private static List<string> alignmentMap = new List<string>();
        private static Coord dotPos = new Coord(0, 0);
        private static int zDotPos = 0;
        private static Axis inputX;
        private static Axis inputY;
        private static Axis inputZ;
        private static InputSimulator inputSimulator = new InputSimulator();

        public static bool calibrationDone = false;


        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);
        /*[DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);
        const UInt32 WM_KEYDOWN = 0x0100;
        const UInt32 WM_KEYUP = 0x101;*/


        [STAThread]
        static void Main(string[] args)
        {
            using (StreamReader r = new StreamReader("indicator.txt", Encoding.Default))
            {
                while (!r.EndOfStream)
                    alignmentMap.Add(r.ReadLine());
            }

            Console.SetWindowSize(120, 38);

            ShowWarningPrompt();

            gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
            joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();

            if (gamepads.Count > 0)
            {
                joystickGuid = gamepads[0].ProductGuid;
                productName = gamepads[0].ProductName;
            }
            else if (joysticks.Count > 0)
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
                    if (dotPos.X + 7 == x && dotPos.Y + 2 == y || (inputZ != null && y == 20 && zDotPos + 7 == x))
                    {
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.Write("O");
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        if (alignmentMap[y][x] == '#')
                            Console.BackgroundColor = ConsoleColor.White;

                        if (((15 - inputX.GetDeadZoneSize(31) + 7 <= x && 15 + inputX.GetDeadZoneSize(31) + 7 >= x)
                            && (6 - inputY.GetDeadZoneSize(12) + 2 <= y && 6 + inputY.GetDeadZoneSize(12) + 2 >= y))
                            || (inputZ != null && y == 20 && 15 - inputZ.GetDeadZoneSize(31) + 7 <= x && 15 + inputZ.GetDeadZoneSize(31) + 7 >= x))
                        {
                            Console.BackgroundColor = ConsoleColor.DarkGray;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }

                        Console.Write(alignmentMap[y][x]);
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;

                    }
                }
                Console.Write("\t\t\t\t\t\t\t");
                Console.WriteLine();
            }
        }

        static void ShowWarningPrompt()
        {
            DrawTitle();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Warning: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("The program will read the axis values of the selected joystick, \n" +
                "and output an alternating pulse of emulated keypresses to your computer. \n" +
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


            Console.Write("  2. Calibrate device");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (calibrationDone)
                Console.Write($"         [done]");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;


            Console.WriteLine("  3. Select control scheme");


            Console.Write("  4. Select pulse frequency");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   [{frequency} Hz]");
            Console.ForegroundColor = ConsoleColor.White;


            Console.WriteLine("  5. Start pulsing");


            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nWhat would you like to do? (1-5): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 5)
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
                    if (joystick != null)
                        CalibrateDevice();
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No gamepad/joystick found.");
                        Console.ForegroundColor = ConsoleColor.White;
                        Thread.Sleep(500);
                        ShowMainMenu();
                    }
                    break;
                case 3:
                    SelectControls();
                    break;
                case 4:
                    SelectFrequency();
                    break;
                case 5:
                    StartPulsing();
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

            string[] supportedInputs = { "X axis", "Y axis", "Z axis", "Pause button" };
            string[] sIInfos = { "left-right", "front-back", "if you can twist the joystick, or have 2 of them", "toggles the pulses" };
            int cInputIndex = 0;
            Dictionary<string, Axis> inputs = new Dictionary<string, Axis>();

            string[] calPositions = { "front left", "back right", "Z left", "Z right" };
            int cCalPosIndex = 0;

            int calibrationPhase = 0;
            calibrationDone = false;

            Console.CursorVisible = false;

            // Poll events from joystick
            while (!calibrationDone)
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
                            Console.WriteLine($"  {i + 1}. {allInputs[i].name} ->  {allInputs[i].cValue}\t\t\t\t\t\t\t\t\t\t");
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
                        dotPos.X = (int)(inputX.GetRatio() * 31);
                        dotPos.Y = (int)Math.Round(inputY.GetRatio() * 12);
                        if(inputZ != null)
                            zDotPos = (int)(inputZ.GetRatio() * 31);
                        break;
                    case 3:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Move the joystick around VERY slightly to select its dead zone.\t\t\t\t\t\t\t");
                        Console.ForegroundColor = ConsoleColor.White;
                        dotPos.X = (int)(inputX.GetRatio() * 31);
                        dotPos.Y = (int)Math.Round(inputY.GetRatio() * 12);
                        if (inputZ != null)
                            zDotPos = (int)(inputZ.GetRatio() * 31);
                        inputX.UpdateDeadZone();
                        inputY.UpdateDeadZone();
                        if (inputZ != null)
                            inputZ.UpdateDeadZone();
                        break;
                }

                Console.WriteLine("\t\t\t\t\t\t\t\t\t\t\t\t");
                foreach (var i in inputs)
                {
                    Console.Write($"{i.Key}: ");
                    if (i.Value == null)
                        Console.Write("ignore");
                    else
                    {
                        Console.Write($"{i.Value.name} -> {i.Value.cValue}");
                        if (i.Value.calibrated) {
                            Console.Write($"\t({Math.Round(i.Value.GetRatio() * 100, 1)}%)");
                            if (i.Value.deadZoneRange > 0)
                                Console.Write($"\tdeadzone: {i.Value.middleValue - i.Value.deadZoneRange} - {i.Value.middleValue + i.Value.deadZoneRange}\t({Math.Round((double)i.Value.deadZoneRange / i.Value.middleValue * 100, 1)}%)");
                            else
                                Console.Write("\t\t\t");
                        }
                    }
                    Console.WriteLine("\t\t\t\t");
                }

                switch (calibrationPhase)
                {
                    case 0:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\nPress the number of your {supportedInputs[cInputIndex]} ({sIInfos[cInputIndex]}) (or press enter to ignore it)  ");
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
                    case 3:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\nPress enter to conclude calibration, or 'R' to reset dead zone.");
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
                                Console.Clear();
                                DrawTitle();
                                break;
                        }
                    }
                    else if (consoleKeyInfo.Key == ConsoleKey.R)
                    {
                        switch (calibrationPhase)
                        {
                            case 2:
                                calibrationPhase = 0;
                                cCalPosIndex = 0;
                                cInputIndex = 0;
                                inputs.Clear();
                                inputX.calibrated = false;
                                inputY.calibrated = false;
                                if (inputZ != null)
                                    inputZ.calibrated = false;

                                Console.Clear();
                                DrawTitle();
                                break;
                            case 3:
                                inputX.deadZoneRange = 0;
                                inputY.deadZoneRange = 0;
                                if (inputZ != null)
                                    inputZ.deadZoneRange = 0;                                                                
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
                                    if (inputZ != null)
                                        zDotPos = 0;

                                    inputX = inputs["X axis"];
                                    inputY = inputs["Y axis"];
                                    inputZ = inputs["Z axis"];
                                }                                
                                break;
                            case 1:
                                switch (cCalPosIndex)
                                {
                                    case 0:
                                        inputX.SetMin();
                                        inputY.SetMin();
                                        dotPos.X = 31;
                                        dotPos.Y = 12;
                                        break;
                                    case 1:
                                        inputX.SetMax();
                                        inputY.SetMax();
                                        break;
                                    case 2:
                                        if (inputZ != null)
                                        {
                                            inputZ.SetMin();
                                            zDotPos = 31;
                                        }
                                        else
                                            cCalPosIndex = 3;
                                        break;
                                    case 3:
                                        inputZ.SetMax();
                                        break;
                                }

                                cCalPosIndex++;
                                if (cCalPosIndex == calPositions.Length)
                                    calibrationPhase++;
                                break;
                            case 2:
                                calibrationPhase++;
                                break;
                            case 3:
                                calibrationPhase++;
                                calibrationDone = true;
                                break;
                        }
                    }
                }

                Thread.Sleep(1000 / frequency);
            }

            Console.CursorVisible = true;

            ShowMainMenu();
        }

        /*static void SelectWindow()
        {

        }*/
        
        static void SelectControls()
        {
            Console.Clear();
            DrawTitle();

            Console.WriteLine("Select control scheme\n");

            Console.WriteLine("  1. X: AD; Y: WS; Z: QE (for rockets)");
            Console.WriteLine("  2. X: QE; Y: WS; Z: AD (for planes)");          

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nChoose your scheme (1-2): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 5)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nInvalid id. Please enter a valid number (1-2).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nChoose your scheme (1-2): ");
                Console.ForegroundColor = ConsoleColor.White;

                id = int.Parse(Console.ReadLine());
            }

            switch (id)
            {
                case 1:
                    inputX.role = Axis.AxisRole.yaw;
                    inputY.role = Axis.AxisRole.pitch;
                    if(inputZ != null)
                        inputZ.role = Axis.AxisRole.roll;
                    break;
                case 2:
                    inputX.role = Axis.AxisRole.roll;
                    inputY.role = Axis.AxisRole.pitch;
                    if (inputZ != null)
                        inputZ.role = Axis.AxisRole.yaw;
                    break;
            }
        }

        static void SelectFrequency()
        {
            Console.Clear();
            DrawTitle();

            Console.WriteLine("Select frequency\n");

            Console.WriteLine("  1. 5   Hz   precision: ultra     (0.5%);  stability: low");
            Console.WriteLine("  2. 10  Hz   precision: very high (1%);    stability: medium");
            Console.WriteLine("  3. 20  Hz   precision: high      (2%);    stability: high"); 
            Console.WriteLine("  4. 50  Hz   precision: medium    (5%);    stability: ?");
            Console.WriteLine("  5. 100 Hz   precision: low       (10%);   stability: ?");


            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nChoose your frequency (1-5): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 5)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nInvalid id. Please enter a valid number (1-5).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nChoose your frequency (1-5): ");
                Console.ForegroundColor = ConsoleColor.White;

                id = int.Parse(Console.ReadLine());
            }

            switch (id)
            {
                case 1:
                    frequency = 5;
                    break;
                case 2:
                    frequency = 10;
                    break;
                case 3:
                    frequency = 20;
                    break;
                case 4:
                    frequency = 50;
                    break;
                case 5:
                    frequency = 100;
                    break;
            }

            ShowMainMenu();
        }

        static void StartPulsing()
        {
            Task.Run(() => { Pulser(inputX); });
            Task.Run(() => { Pulser(inputY); });
            Task.Run(() => { Pulser(inputZ); });
        }

        static void Pulser(Axis axis)
        {
            int baseDelay = 1000 / frequency;
            VirtualKeyCode cKey = VirtualKeyCode.VK_0; 

            while (true)
            {
                double ratio = axis.GetRatio();
                int onDuration;

                if (ratio != 0.5)
                {
                    if (ratio < 0.5)
                    {
                        onDuration = (int)Math.Round(baseDelay * (0.5 - ratio) * 2);
                        switch (axis.role)
                        {
                            case Axis.AxisRole.yaw:
                                cKey = VirtualKeyCode.VK_A;
                                break;
                            case Axis.AxisRole.pitch:
                                cKey = VirtualKeyCode.VK_W;
                                break;
                            case Axis.AxisRole.roll:
                                cKey = VirtualKeyCode.VK_Q;
                                break;
                        }
                    }
                    else
                    {
                        onDuration = (int)Math.Round(baseDelay * (ratio - 0.5) * 2);
                        switch (axis.role)
                        {
                            case Axis.AxisRole.yaw:
                                cKey = VirtualKeyCode.VK_D;
                                break;
                            case Axis.AxisRole.pitch:
                                cKey = VirtualKeyCode.VK_S;
                                break;
                            case Axis.AxisRole.roll:
                                cKey = VirtualKeyCode.VK_E;
                                break;
                        }
                    }
                    inputSimulator.Keyboard.KeyDown(cKey);
                    Thread.Sleep(onDuration);
                    inputSimulator.Keyboard.KeyUp(cKey);
                    Thread.Sleep(baseDelay - onDuration);
                }
                else
                    Thread.Sleep(baseDelay);
            }
        }
    }
}