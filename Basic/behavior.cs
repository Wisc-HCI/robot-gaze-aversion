using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aldebaran.Proxies;

namespace Basic
{
    public class behavior
    {
        private BehaviorManagerProxy manager;
        private string ip;
        private int port;

        public behavior()
        {

        }

        public behavior(string ip,int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public bool start(string behaviorName)
        {
            try
            {
                manager = new BehaviorManagerProxy(ip, port);
                manager.runBehavior(behaviorName);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
    }
}
