using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoystickAxisPulsator
{
    public class Input
    {
        public string name;
        public string rawName;
        public int value;
        public bool calibrated = false;

        public Input() { }

        public Input(string rawName, int value)
        {
            this.rawName = rawName;
            this.value = value;
        }
    }
}
