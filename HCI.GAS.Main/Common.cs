using HCI.GAS.Main.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.GAS.Main
{
    public class Common
    {
        public readonly NaoStatus naoStatus;
        public bool kinectStatus;
        public readonly Hashtable OpenedWindow;
        public readonly Event.MessageBus EventMessage;

        public Common()
        {
            kinectStatus = false;
            naoStatus = new NaoStatus();
            OpenedWindow = new Hashtable();
            EventMessage = new Event.MessageBus();
        }
    }
}
