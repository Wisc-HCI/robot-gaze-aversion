using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aldebaran.Proxies;

namespace Basic
{
    public class tools
    {
        private string ip;
        private int port;
        public tools(string ip, string port)
        {
            try
            {
                this.ip = ip;
                this.port = Convert.ToInt32(port);

            }
            catch (Exception)
            {
                throw new Exception();
            }
        }

        public tools(string ip, int port)
        {
            try
            {
                this.ip = ip;
                this.port = port;

            }
            catch (Exception)
            {
                throw new Exception();
            }
        }

        public bool tryConnection()
        {
            try
            {
                MemoryProxy mem = new MemoryProxy(this.ip, this.port);
                return true;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.ToString());
                return false;
            }
        }
    }
}
