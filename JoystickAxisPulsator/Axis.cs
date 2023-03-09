using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoystickAxisPulsator
{
    public class Axis
    {
        public string name;
        public AxisRole role;
        public int cValue;
        public int maxValue;
        public int minValue;
        public int middleValue;
        public int deadZoneRange = 0;
        public bool inverted = false;
        public bool calibrated = false;

        public enum AxisRole { yaw, pitch, roll }


        public Axis(string name, int value) 
        { 
            this.name = name;
            this.cValue = value;
        }

        public void UpdateDeadZone() {
            if (Math.Abs(cValue - middleValue) > deadZoneRange)
                deadZoneRange = Math.Abs(cValue - middleValue);
        }

        public void SetMin() { minValue = cValue; }
        public void SetMax() { 
            maxValue = cValue; 
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

            if (deadZoneRange == 0 || !Program.calibrationDone || cValue < middleValue - deadZoneRange || cValue > middleValue + deadZoneRange)
                result = (double)cValue / maxValue - minValue;

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