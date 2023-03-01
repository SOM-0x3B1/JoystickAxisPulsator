using System;
using System.Collections.Generic;
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

        public Axis(string name, int value) 
        { 
            this.name = name;
            this.cValue = value;
        }
    }
}
