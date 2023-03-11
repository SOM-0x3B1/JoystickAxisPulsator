using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoystickAxisPulsator
{
    public class Button : Input
    {        
        public int highValue;
        public int lowValue;


        public Button(string name, string rawName, int value)
        {
            this.name = name;
            this.rawName = rawName;
            this.value = value;
        }

        public void SetLow() {
            lowValue = value;
        }
        public void SetHigh()
        {
            highValue = value;
        }
    }
}
