using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.GAS.Main.Event
{
    class Messenger
    {
        private string _name;
        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public Messenger(string name)
        {
            this.name = name;
        }
    }
}
