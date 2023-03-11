using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
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
        private static int frequency = 5;
        private static List<string> alignmentMap = new List<string>();
        private static Coord dotPos = new Coord(0, 0);
        private static int zDotPos = 0;
        private static List<Input> allInputs = new List<Input>();
        private static Dictionary<string, int> registeredInputs = new Dictionary<string, int>();
        private static Dictionary<string, Axis> AxesByRawName = new Dictionary<string, Axis>();
        private static Axis inputX;
        private static Axis inputY;
        private static Axis inputZ;
        private static Button pauseButton;
        private static string rawPuseButtonName = "";
        private static int cCalPosIndex = 0;
        private static int calibrationPhase = 0;
        private static InputSimulator inputSimulator = new InputSimulator();
        private static string controlScheme = "";

        public static bool calibrationDone = false;
        public static bool pulsing = false;


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
            Console.Title = "Joystick Axis Pulsator";

            using (StreamReader r = new StreamReader("indicator.txt", Encoding.Default))
            {
                while (!r.EndOfStream)
                    alignmentMap.Add(r.ReadLine());
            }

            Console.SetWindowSize(120, 38);
            Console.SetBufferSize(120, 38);
            AdvancedDisplay.DisableConsoleQuickEdit();

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
            Console.WriteLine(" ======================================");
            Console.WriteLine(" ||   Joystick Axis Pulsator v0.1.0  ||");
            Console.WriteLine(" ||          by SOM-0x3B1            ||");
            Console.WriteLine(" ======================================\n");
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
            Console.Write(" Warning: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" The program will read the axis values of the selected joystick, \n" +
                " and output an alternating pulse of emulated keypresses to your computer. \n" +
                " Since the app will spam these presses at a very high rate,");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" they may cause some \n" +
                " unexpected issues with your applications and system.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n By accepting the following prompt, you acknowledge that I (SOM-0x3B1) do not\n" +
                " take responsibility for (but will gladly help with) any problems that this \n" +
                " software might cause. \n\n" +
                " Use this software at your own risk.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n\n Do you accept these terms? [Y/N]: ");
            Console.ForegroundColor = ConsoleColor.White;

            string input = Console.ReadLine().ToLower();
            while (input != "n" && input != "y")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Invalid format. Please use 'Y' or 'N'.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(" Do you accept these terms? [Y/N]: ");
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

            Console.WriteLine(" Main menu\n");


            Console.Write("   1. Select gamepad/joystick");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (joystickGuid == Guid.Empty)
                Console.WriteLine($"  [none]");
            else
                Console.WriteLine($"  [{productName}]");
            Console.ForegroundColor = ConsoleColor.White;


            Console.Write("   2. Calibrate device");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (calibrationDone)
                Console.Write($"         [done]");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;


            Console.Write("   3. Select control scheme");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if(controlScheme != "")
                Console.Write($"    [{controlScheme}]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();


            Console.Write("   4. Select pulse frequency");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   [{frequency} Hz]");
            Console.ForegroundColor = ConsoleColor.White;


            Console.WriteLine("   5. Start pulsing");


            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n What would you like to do? (1-5): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 5)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Invalid id. Please enter a valid number (1-4).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n What would you like to do? (1-5): ");
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
                        Console.WriteLine(" No gamepad/joystick found.");
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
                    if (joystick != null && controlScheme != "" && calibrationDone)
                        StartPulsing();
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        if(joystick == null)
                            Console.WriteLine(" No gamepad/joystick found.");
                        else if(controlScheme == "")
                            Console.WriteLine(" Controls not defined.");
                        else if (!calibrationDone)
                            Console.WriteLine(" Missing calibration.");
                        Console.ForegroundColor = ConsoleColor.White;
                        Thread.Sleep(500);
                        ShowMainMenu();
                    }
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

            Console.WriteLine("\n Searching for compatible devices...");

            joystickGuid = Guid.Empty;
            gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
            joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();

            // If Joystick not found, throws an error
            while (gamepads.Count + joysticks.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" No gamepad/joystick found.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" Press enter to retry.");

                Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" Searching for compatible devices...");
                gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).ToList();
                joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices).ToList();
            }

            Console.WriteLine("\n Devices found:");

            Console.WriteLine("\n   Gamepads:");

            for (int i = 0; i < gamepads.Count; i++)
                Console.WriteLine($"\t{i + 1}. {gamepads[i].ProductName}");
            if (gamepads.Count == 0)
                Console.WriteLine("\t-");

            Console.WriteLine("\n   Joysticks:");
            for (int i = gamepads.Count; i < joysticks.Count; i++)
                Console.WriteLine($"\t{i + 1}. {joysticks[i - gamepads.Count].ProductName}");
            if (joysticks.Count == 0)
                Console.WriteLine("\t-");


            while (joystickGuid == Guid.Empty)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n\n Select the number of your gamepad/joystick: ");
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
                    Console.WriteLine($"\n Invalid id. Please enter a valid number (1-{joysticks.Count + gamepads.Count}).");
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
            string[] supportedInputs = { "X axis", "Y axis", "Z axis", "Pause button" };
            string[] sIInfos = { "left-right", "front-back", "if you can twist the joystick, or have 2 of them", "toggles the pulses" };
            int cInputIndex = 0;
            Dictionary<string, Axis> AxesByAxisName = new Dictionary<string, Axis>();
            

            string[] calPositions = { "front left", "back right", "Z left", "Z right" };
            
            calibrationDone = false;

            Console.CursorVisible = false;

            // Poll events from joystick
            while (!calibrationDone)
            {
                joystick.Poll();
                JoystickUpdate[] datas = joystick.GetBufferedData();
                foreach (JoystickUpdate state in datas)
                {
                    string rawInputName = state.Offset.ToString();
                    if (calibrationPhase == 0)
                    {
                        if (!registeredInputs.ContainsKey(rawInputName))
                        {
                            registeredInputs.Add(rawInputName, allInputs.Count);
                            allInputs.Add(new Input(rawInputName, state.Value));
                        }
                        else
                            allInputs[registeredInputs[rawInputName]].value = state.Value;
                    }
                    else
                    {
                        if(rawPuseButtonName == rawInputName)
                            pauseButton.value = state.Value;
                        else if (AxesByRawName.ContainsKey(rawInputName))
                            AxesByRawName[rawInputName].value = state.Value;
                    }
                }

                Console.SetCursorPosition(0, 5);

                switch (calibrationPhase)
                {
                    case 0:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(" Move your joystick around to detect input types.\n");
                        Console.ForegroundColor = ConsoleColor.White;

                        Console.WriteLine(" Inputs detected: \n");
                        for (int i = 0; i < allInputs.Count; i++)
                            Console.WriteLine($"  {i + 1}. {allInputs[i].rawName} ->  {allInputs[i].value}\t\t\t\t\t\t\t\t\t\t");
                        break;
                    case 1:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($" Set your joystick to its {calPositions[cCalPosIndex]} position, then press enter.\t\t");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 2:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($" Move your joystick around to test its configuration, then set it to its default (middle) position.");
                        Console.ForegroundColor = ConsoleColor.White;
                        dotPos.X = (int)(inputX.GetRatio() * 31);
                        dotPos.Y = (int)Math.Round(inputY.GetRatio() * 12);
                        if(inputZ != null)
                            zDotPos = (int)(inputZ.GetRatio() * 31);
                        break;
                    case 3:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(" Move the joystick around VERY slightly to select its dead zone.\t\t\t\t\t\t");
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
                    case 4:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(" Make sure that your pause button is not pressed.\t\t\t\t\t\t");
                        Console.ForegroundColor = ConsoleColor.White;
                        dotPos.X = (int)(inputX.GetRatio() * 31);
                        dotPos.Y = (int)Math.Round(inputY.GetRatio() * 12);
                        if (inputZ != null)
                            zDotPos = (int)(inputZ.GetRatio() * 31);
                        break;
                    case 5:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(" Keep your pause button pressed, and press enter.\t\t\t\t\t\t");
                        Console.ForegroundColor = ConsoleColor.White;
                        dotPos.X = (int)(inputX.GetRatio() * 31);
                        dotPos.Y = (int)Math.Round(inputY.GetRatio() * 12);
                        if (inputZ != null)
                            zDotPos = (int)(inputZ.GetRatio() * 31);
                        break;
                }

                Console.WriteLine("\t\t\t\t\t\t\t\t\t\t\t\t");
                foreach (var i in AxesByRawName)
                {
                    Console.Write($" {i.Value.name}: ");
                    if (i.Value == null)
                        Console.Write("ignore");
                    else
                    {
                        Console.Write($"{i.Value.rawName}\t");
                        if (calibrationPhase > 0) {
                            Console.Write($" -> {i.Value.value}");
                            if (i.Value.calibrated) {
                                Console.Write($"\t({Math.Round(i.Value.GetRatio() * 100, 1)}%)");
                                if (i.Value.deadZoneRange > 0)
                                    Console.Write($"\tdeadzone: {i.Value.middleValue - i.Value.deadZoneRange} - {i.Value.middleValue + i.Value.deadZoneRange}\t({Math.Round((double)i.Value.deadZoneRange / i.Value.middleValue * 100, 1)}%)");
                                else
                                    Console.Write("\t\t\t");
                            }
                        }
                    }
                    Console.WriteLine("\t\t\t\t");
                }
                Console.Write($" Pause: ");
                if (pauseButton == null)
                    Console.Write("ignore");
                else
                    Console.Write($"{pauseButton.rawName} -> {pauseButton.value}");
                Console.WriteLine("\t\t\t\t");



                switch (calibrationPhase)
                {
                    case 0:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\n Press the number of your {supportedInputs[cInputIndex]} ({sIInfos[cInputIndex]}) (or press enter to ignore it)\t\t\t\t\t\t\t\t\t\t\t");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 1:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\n Press enter to confirm.");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 2:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\n Press enter to proceed, or 'R' to reconfigure.");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 3:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\n Press enter to proceed, or 'R' to reset dead zone.");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 4:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\n Press enter to proceed.\t\t\t\t\t");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case 5:
                        DrawAignmentMap();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\t\t\t\t\t\t\t\t\t\t\t\t\n Press enter to conclude calibration, or 'R' to reconfigure.");
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
                                Input cInput = allInputs[int.Parse(consoleKeyInfo.KeyChar.ToString()) - 1];
                                if (cInputIndex < 3)
                                {
                                    AxesByAxisName[supportedInputs[cInputIndex]] = new Axis(supportedInputs[cInputIndex], cInput.rawName, cInput.value);
                                    if (AxesByAxisName[supportedInputs[cInputIndex]] != null)
                                        AxesByRawName[cInput.rawName] = AxesByAxisName[supportedInputs[cInputIndex]];
                                }
                                else
                                {
                                    pauseButton = new Button(supportedInputs[cInputIndex], cInput.rawName, cInput.value);
                                    rawPuseButtonName = cInput.rawName;

                                    calibrationPhase++;
                                    dotPos.X = 0;
                                    dotPos.Y = 0;
                                    if (inputZ != null)
                                        zDotPos = 0;

                                    inputX = AxesByAxisName["X axis"];
                                    inputY = AxesByAxisName["Y axis"];
                                    inputZ = AxesByAxisName["Z axis"];
                                }
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
                                AxesByAxisName.Clear();
                                AxesByRawName.Clear();
                                inputX.calibrated = false;
                                inputY.calibrated = false;
                                if (inputZ != null)
                                    inputZ.calibrated = false;
                                pauseButton.calibrated = false;

                                Console.Clear();
                                DrawTitle();
                                break;
                            case 3:
                                inputX.deadZoneRange = 0;
                                inputY.deadZoneRange = 0;
                                if (inputZ != null)
                                    inputZ.deadZoneRange = 0;                                                                
                                break;
                            case 4:
                                calibrationPhase = 0;
                                cCalPosIndex = 0;
                                cInputIndex = 0;
                                AxesByAxisName.Clear();
                                AxesByRawName.Clear();
                                inputX.calibrated = false;
                                inputY.calibrated = false;
                                if (inputZ != null)
                                    inputZ.calibrated = false;
                                pauseButton.calibrated = false;

                                Console.Clear();
                                DrawTitle();
                                break;
                        }
                    }
                    else if (consoleKeyInfo.Key == ConsoleKey.Enter)
                    {
                        switch (calibrationPhase)
                        {
                            case 0:
                                if (cInputIndex < 3)
                                    AxesByAxisName[supportedInputs[cInputIndex]] = null;
                                else
                                    pauseButton = null;
                                cInputIndex++;
                                if (cInputIndex == supportedInputs.Length)
                                {
                                    calibrationPhase++;
                                    dotPos.X = 0;
                                    dotPos.Y = 0;
                                    if (inputZ != null)
                                        zDotPos = 0;

                                    inputX = AxesByAxisName["X axis"];
                                    inputY = AxesByAxisName["Y axis"];
                                    inputZ = AxesByAxisName["Z axis"];

                                    Console.Clear();
                                    DrawTitle();
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
                                        if (inputZ == null)
                                            cCalPosIndex += 2;
                                        break;
                                    case 2:
                                        if (inputZ != null) { 
                                            inputZ.SetMin();
                                            zDotPos = 31;
                                        }  
                                        break;
                                    case 3:
                                        inputZ.SetMax();
                                        break;
                                }

                                cCalPosIndex++;
                                if (cCalPosIndex >= calPositions.Length)
                                    calibrationPhase++;
                                break;
                            case 2:
                                calibrationPhase++;
                                break;
                            case 3:
                                calibrationPhase++;
                                
                                inputX.role = Axis.AxisRole.yaw;
                                inputY.role = Axis.AxisRole.pitch;
                                if (inputZ != null)
                                    inputZ.role = Axis.AxisRole.roll;
                                controlScheme = "rocket";

                                if (pauseButton == null)
                                    calibrationDone = true;

                                break;
                            case 4:
                                pauseButton.SetLow();
                                calibrationPhase++;
                                break;
                            case 5:
                                pauseButton.SetHigh();
                                calibrationDone = true;
                                break;
                        }
                    }
                }
                Thread.Sleep(10);
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

            Console.WriteLine(" Select control scheme\n");

            Console.WriteLine("   1. X: A-D; Y: W-S; Z: Q-E (for rockets)");
            Console.WriteLine("   2. X: Q-E; Y: W-S; Z: A-D (for planes)");          

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n Choose your scheme (1-2): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 5)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Invalid id. Please enter a valid number (1-2).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n Choose your scheme (1-2): ");
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
                    controlScheme = "rocket";
                    break;
                case 2:
                    inputX.role = Axis.AxisRole.roll;
                    inputY.role = Axis.AxisRole.pitch;
                    if (inputZ != null)
                        inputZ.role = Axis.AxisRole.yaw;
                    controlScheme = "plane";
                    break;
            }

            ShowMainMenu();
        }

        static void SelectFrequency()
        {
            Console.Clear();
            DrawTitle();

            Console.WriteLine(" Select frequency\n");

            Console.WriteLine("   1. 5   Hz   precision: ultra     (0.5%);  stability: high");
            Console.WriteLine("   2. 10  Hz   precision: very high (1%);    stability: medium");
            Console.WriteLine("   3. 20  Hz   precision: high      (2%);    stability: low"); 


            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n Choose your frequency (1-3): ");
            Console.ForegroundColor = ConsoleColor.White;

            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 3)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Invalid id. Please enter a valid number (1-3).");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n Choose your frequency (1-3): ");
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
            pulsing = true;
            int statusLine = Console.CursorTop + 1;
            string[] statuses = { "", ".", "..", "..." };
            int statusDelay = 0;
            int statusIndex = 0;
            Console.WriteLine("\n Running...");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n Press ESC to exit.");
            Console.ForegroundColor = ConsoleColor.White;

            _ = Task.Run(() => { Pulser(inputX); });
            _ = Task.Run(() => { Pulser(inputY); });
            if(inputZ != null)
                _ = Task.Run(() => { Pulser(inputZ); });

            Console.CursorVisible = false;

            while (pulsing)
            {
                joystick.Poll();
                JoystickUpdate[] datas = joystick.GetBufferedData();
                foreach (JoystickUpdate state in datas)
                {
                    string rawInputName = state.Offset.ToString();
                    if (rawPuseButtonName == rawInputName)
                    {
                        pauseButton.value = state.Value;
                        if (pauseButton.value == pauseButton.highValue)
                            pulsing = false;
                    }
                    else if (AxesByRawName.ContainsKey(rawInputName))
                        AxesByRawName[rawInputName].value = state.Value;
                }
                Thread.Sleep(1000 / frequency);

                statusDelay++;
                if (statusDelay > 5)
                {
                    Console.SetCursorPosition(0, statusLine);
                    Console.WriteLine(" Running" + statuses[statusIndex] + "    ");
                    statusIndex++;
                    if (statusIndex > 3)
                        statusIndex = 0;
                    statusDelay = 0;
                }

                ConsoleKeyInfo consoleKeyInfo;
                if (Console.KeyAvailable)
                {
                    while (Console.KeyAvailable)
                        consoleKeyInfo = Console.ReadKey(true);
                    consoleKeyInfo = Console.ReadKey(true);
                    if (consoleKeyInfo.Key == ConsoleKey.Escape)
                        pulsing = false;
                }
            }

            Console.CursorVisible = true;

            ShowMainMenu();
        }

        static void Pulser(Axis axis)
        {
            int baseDelay = 1000 / frequency;
            VirtualKeyCode cKey = VirtualKeyCode.VK_0; 

            while (pulsing)
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
                                if(controlScheme == "rocket")
                                    cKey = VirtualKeyCode.VK_D;
                                else
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
                                if (controlScheme == "rocket")
                                    cKey = VirtualKeyCode.VK_A;
                                else
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