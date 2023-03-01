using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoystickAxisPulsator
{
    public class Axis
    {
        public int id = 0;
        public int cValue;
        public int maxValue;
        public int minValue;

        public Axis(int id, int value) 
        { 
            this.id = id;
            this.cValue = value;
        }
    }
}
