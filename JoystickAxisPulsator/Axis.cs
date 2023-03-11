using System;

namespace JoystickAxisPulsator
{
    public class Axis : Input
    {
        public AxisRole role;
        public int maxValue;
        public int minValue;
        public int middleValue;
        public int deadZoneRange = 0;
        public bool inverted = false;

        public enum AxisRole { yaw, pitch, roll }


        public Axis(string name, string rawName, int value) 
        { 
            this.name = name;
            this.rawName = rawName;
            this.value = value;
        }

        public void UpdateDeadZone() {
            if (Math.Abs(value - middleValue) > deadZoneRange)
                deadZoneRange = Math.Abs(value - middleValue);
        }

        public void SetMin() { minValue = value; }
        public void SetMax() { 
            maxValue = value; 
            if(minValue > maxValue)
            {
                inverted = true;
                int m = minValue;
                minValue = maxValue;
                maxValue = m;                
            }
            middleValue = (maxValue + minValue) / 2;
            calibrated = true;
        }


        public double GetRatio()
        {
            double result = 0.5;

            if (deadZoneRange == 0 || !Program.calibrationDone || value < middleValue - deadZoneRange || value > middleValue + deadZoneRange)
                result = (double)(value - minValue) / (maxValue - minValue);
            if (result < 0)
                result = 0;
            else if (result > 1)
                result = 1;

            if (inverted)
                return 1 - result;
            else
                return result;
        }

        public int GetDeadZoneSize(int screenSize)
        {
            return (int)Math.Round((double)deadZoneRange / middleValue * ((double)screenSize / 2));
        }
    }
}    