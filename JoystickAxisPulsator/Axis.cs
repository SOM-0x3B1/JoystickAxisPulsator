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
        public int cValue;
        public int maxValue;
        public int minValue;
        public int middleValue;
        public int deadZoneRange = 0;

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
        public void SetMax() { maxValue = cValue; }


        public double GetPercent() { return (double)cValue / maxValue - minValue; }
    }
}    