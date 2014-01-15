using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Basic;
namespace Basic.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Basic.tracker tk = new Basic.tracker("192.168.1.112", 9559);
        }

        [TestMethod]
        public void ledTest()
        {
            Basic.Leds led = new Basic.Leds("192.168.1.112", 9559);
        }
    }
}
