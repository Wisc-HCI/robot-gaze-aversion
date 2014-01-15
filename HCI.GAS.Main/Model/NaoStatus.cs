using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.GAS.Main.Model
{
    public class NaoStatus
    {
        private string _ip;
        public string ip
        {
            set
            {
                _ip = value;
            }
            get
            {
                return _ip;
            }
        }
        private int _port;
        public int port
        {
            set
            {
                _port = value;
            }
            get
            {
                return _port;
            }
        }
        private bool _status;
        public bool status
        {
            set
            {
                _status = value;
            }
            get
            {
                return _status;
            }
        }
        private int _condition;
        public int condition
        {
            set
            {
                _condition = value;
            }
            get
            {
                return _condition;
            }
        }

    }
}
