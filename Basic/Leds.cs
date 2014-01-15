using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Aldebaran.Proxies;

namespace Basic
{
    public class Leds
    {
        public Leds(string ip, int port)
        {
            LedsProxy led = new LedsProxy(ip, port);
            led.on("ChestLedsRed");
            Thread.Sleep(2000);
            led.on("ChestLedsGreen");
            Thread.Sleep(2000);
            led.off("ChestLeds");
        }

    }
}
