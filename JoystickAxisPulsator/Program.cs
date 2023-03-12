using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput.Native;
using WindowsInput;

namespace JoystickAxisPulsator
{
    internal class Program
    {
        private static IList<DeviceInstance> gamepads;
        private static IList<DeviceInstance> joysticks;
        private static DirectInput directInput = new DirectInput();
        private static Guid joystickGuid = Guid.Empty;
        private static Joystick joystick;
        private static string productName = "";        

        private static List<string> alignmentMap = new List<string>();
        private static Coord XYdotPos = new Coord(0, 0);
        private static int ZDotPos = 0;

        private static List<Input> allInputs = new List<Input>();
        private static Dictionary<string, int> registeredInputsByRawName = new Dictionary<string, int>();
        private static Dictionary<string, Axis> axesByRawName = new Dictionary<string, Axis>();
        private static Axis inputX;
        private static Axis inputY;
        private static Axis inputZ;
        private static Button pauseButton;
        private static string puseButtonRawName = "";

        private static int calibrationPhase = 0;
        private static int cCalPosIndex = 0;
        public static bool calibrationDone = false;

        private static InputSimulator inputSimulator = new InputSimulator();
        private static string controlScheme = "";
        private static int frequency = 10;
        public static bool pulsing = false;

        private static string tabSpace = "\t\t\t\t\t\t"; 
        private static string bigTabSpace = "\t\t\t\t\t\t\t\t\t\t"; 
        private static string gigaTabSpace = "\t\t\t\t\t\t\t\t\t\t\t\t\t"; 


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

            gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);
            joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices);

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
            Console.WriteLine(" ||   Joystick Axis Pulsator v0.1.1  ||");
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
                    if (XYdotPos.X + 7 == x && XYdotPos.Y + 2 == y || (inputZ != null && y == 20 && ZDotPos + 7 == x))
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

        static void WriteColoredText(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.White;
        }        
        static void WriteLineColoredText(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }



        static void ShowWarningPrompt()
        {
            DrawTitle();

            WriteColoredText(" Warning: ", ConsoleColor.Green);
            Console.Write("The application will read the axis values of the selected joystick, \n" +
                " and output an alternating pulse of emulated keypresses to your computer. \n" +
                " Since the it will spam these presses at a very high rate, it may cause some \n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" unexpected issues with your applications and system.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n By accepting the following prompt, you acknowledge that I (SOM-0x3B1) do not\n" +
                " take responsibility for (but will gladly help with) any problem this \n" +
                " software might cause. \n\n" +
                " Proceed at your own risk.");

            WriteColoredText("\n\n Do you accept these terms? [Y/N]: ", ConsoleColor.Green);

            string input = Console.ReadLine().ToLower();
            while (input != "n" && input != "y")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Invalid format. Please use 'Y' or 'N'.");

                WriteColoredText("\n\n Do you accept these terms? [Y/N]: ", ConsoleColor.Green);

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


            Console.WriteLine("   6. Exit");


            WriteColoredText("\n What would you like to do? (1-6): ", ConsoleColor.Green);


            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 6)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Invalid id. Please enter a valid number (1-6).");

                WriteColoredText("\n What would you like to do? (1-6): ", ConsoleColor.Green);

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
                        WriteColoredText(" No gamepad/joystick found.", ConsoleColor.Red);
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
                case 6:
                    Environment.Exit(0);
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
            gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);
            joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices);

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
                gamepads = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);
                joysticks = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices);
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

            calibrationDone = false;

            string[] supportedInputs = { "X axis", "Y axis", "Z axis", "Pause button" };
            string[] sIInfos = { "left-right", "front-back", "if you can twist the joystick, or have 2 of them", "toggles the pulses" };
            string[] calPositions = { "front left", "back right", "Z left", "Z right" };
            int cInputIndex = 0;
            Dictionary<string, Axis> AxesByAxisName = new Dictionary<string, Axis>();      

            Console.CursorVisible = false;

            
            while (!calibrationDone)
            {
                joystick.Poll();
                JoystickUpdate[] datas = joystick.GetBufferedData();
                foreach (JoystickUpdate state in datas)
                {
                    string rawInputName = state.Offset.ToString();
                    if (calibrationPhase == 0)
                    {
                        if (!registeredInputsByRawName.ContainsKey(rawInputName))
                        {
                            registeredInputsByRawName.Add(rawInputName, allInputs.Count);
                            allInputs.Add(new Input(rawInputName, state.Value));
                        }
                        else
                            allInputs[registeredInputsByRawName[rawInputName]].value = state.Value;
                    }
                    else
                    {
                        if(puseButtonRawName == rawInputName)
                            pauseButton.value = state.Value;
                        else if (axesByRawName.ContainsKey(rawInputName))
                            axesByRawName[rawInputName].value = state.Value;
                    }
                }

                Console.SetCursorPosition(0, 5);


                if(calibrationPhase > 1)
                {
                    XYdotPos.X = (int)(inputX.GetRatio() * 31);
                    XYdotPos.Y = (int)Math.Round(inputY.GetRatio() * 12);
                    if (inputZ != null)
                        ZDotPos = (int)(inputZ.GetRatio() * 31);

                    if(calibrationPhase == 3)
                    {
                        inputX.UpdateDeadZone();
                        inputY.UpdateDeadZone();
                        if (inputZ != null)
                            inputZ.UpdateDeadZone();
                    }
                }

                switch (calibrationPhase)
                {
                    case 0:
                        WriteLineColoredText(" Move your joystick around to detect input types.\n", ConsoleColor.Green);
                        Console.WriteLine(" Inputs detected: \n");
                        for (int i = 0; i < allInputs.Count; i++)
                            Console.WriteLine($"  {i + 1}. {allInputs[i].rawName} ->  {allInputs[i].value}" + bigTabSpace);
                        break;
                    case 1:
                        WriteLineColoredText($" Set your joystick to its {calPositions[cCalPosIndex]} position, then press enter.\t\t", ConsoleColor.Green);
                        break;
                    case 2:
                        WriteLineColoredText(" Move your joystick around to test its configuration, then set it to its default (middle) position.", ConsoleColor.Green);
                        break;
                    case 3:
                        WriteLineColoredText(" Move the joystick around VERY slightly to select its dead zone." + tabSpace, ConsoleColor.Green);
                        break;
                    case 4:
                        WriteLineColoredText(" Make sure that your pause button is not pressed." + tabSpace, ConsoleColor.Green);
                        break;
                    case 5:
                        WriteLineColoredText(" Keep your pause button pressed, and press enter." + tabSpace, ConsoleColor.Green);
                        break;
                }

                Console.WriteLine(gigaTabSpace);
                foreach (var i in axesByRawName)
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
                    Console.WriteLine(tabSpace);
                }
                if (pauseButton != null)
                {
                    Console.Write($" Pause: {pauseButton.rawName} -> {pauseButton.value}");
                    Console.WriteLine(tabSpace);
                }


                if(calibrationPhase > 0)
                    DrawAignmentMap();

                switch (calibrationPhase)
                {
                    case 0:
                        WriteColoredText(gigaTabSpace + $"\n Press the number of your {supportedInputs[cInputIndex]} ({sIInfos[cInputIndex]}) (or press enter to ignore it)" + bigTabSpace, ConsoleColor.Green);
                        break;
                    case 1:                        
                        WriteColoredText(bigTabSpace + "\n Press enter to confirm.", ConsoleColor.Green);
                        break;
                    case 2:
                        WriteColoredText(bigTabSpace + "\n Press enter to proceed, or 'R' to reconfigure.", ConsoleColor.Green);
                        break;
                    case 3:
                        WriteColoredText(bigTabSpace + "\n Press enter to proceed, or 'R' to reset dead zone.", ConsoleColor.Green);
                        break;
                    case 4:
                        WriteColoredText(bigTabSpace + "\n Press enter to proceed." + tabSpace, ConsoleColor.Green);
                        break;
                    case 5:
                        WriteColoredText(bigTabSpace + "\n Press enter to conclude calibration, or 'R' to reconfigure.", ConsoleColor.Green);
                        break;
                }
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);


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
                                        axesByRawName[cInput.rawName] = AxesByAxisName[supportedInputs[cInputIndex]];
                                }
                                else
                                {
                                    pauseButton = new Button(supportedInputs[cInputIndex], cInput.rawName, cInput.value);
                                    puseButtonRawName = cInput.rawName;

                                    calibrationPhase++;
                                    XYdotPos.X = 0;
                                    XYdotPos.Y = 0;
                                    if (inputZ != null)
                                        ZDotPos = 0;

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
                                axesByRawName.Clear();
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
                                axesByRawName.Clear();
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
                                    XYdotPos.X = 0;
                                    XYdotPos.Y = 0;
                                    if (inputZ != null)
                                        ZDotPos = 0;

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
                                        XYdotPos.X = 31;
                                        XYdotPos.Y = 12;
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
                                            ZDotPos = 31;
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

            WriteColoredText("\n Choose your scheme (1-2): ", ConsoleColor.Green);


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

            Console.WriteLine("   1. 5   Hz   precision: ultra     (0.5%);  stability: low");
            Console.WriteLine("   2. 10  Hz   precision: very high (1%);    stability: medium");
            Console.WriteLine("   3. 20  Hz   precision: high      (2%);    stability: high"); 
            Console.WriteLine("   4. 50  Hz   precision: medium    (5%);    stability: ?"); 

            WriteColoredText("\n Choose your frequency (1-4): ", ConsoleColor.Green);


            int id = int.Parse(Console.ReadLine());
            while (id < 1 || id > 4)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n Invalid id. Please enter a valid number (1-4).");

                WriteColoredText("\n Choose your frequency (1-4): ", ConsoleColor.Green);

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
            string[] statusUpdates = { "", ".", "..", "..." };
            int statusUpdateDelay = 0;
            int statusUpdateIndex = 0;

            Console.WriteLine("\n Running...");

            WriteLineColoredText("\n Press ESC to exit.", ConsoleColor.Green);

            Console.CursorVisible = false;


            _ = Task.Run(() => { Pulser(inputX); });
            _ = Task.Run(() => { Pulser(inputY); });
            if(inputZ != null)
                _ = Task.Run(() => { Pulser(inputZ); });
            

            while (pulsing)
            {
                joystick.Poll();
                JoystickUpdate[] datas = joystick.GetBufferedData();
                foreach (JoystickUpdate state in datas)
                {
                    string rawInputName = state.Offset.ToString();

                    if (axesByRawName.ContainsKey(rawInputName))
                        axesByRawName[rawInputName].value = state.Value;
                    else if(puseButtonRawName == rawInputName)
                    {
                        pauseButton.value = state.Value;
                        if (pauseButton.value == pauseButton.highValue)
                            pulsing = false;
                    }
                }
                Thread.Sleep(1000 / frequency);


                statusUpdateDelay++;
                if (statusUpdateDelay > 5)
                {
                    Console.SetCursorPosition(0, statusLine);
                    Console.WriteLine(" Running" + statusUpdates[statusUpdateIndex] + "    ");
                    statusUpdateIndex++;
                    if (statusUpdateIndex > 3)
                        statusUpdateIndex = 0;
                    statusUpdateDelay = 0;
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
                                cKey = VirtualKeyCode.VK_A;
                                break;
                            case Axis.AxisRole.pitch:
                                if (controlScheme == "rocket")
                                    cKey = VirtualKeyCode.VK_S;
                                else
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
                                if (controlScheme == "rocket")
                                    cKey = VirtualKeyCode.VK_W;
                                else
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