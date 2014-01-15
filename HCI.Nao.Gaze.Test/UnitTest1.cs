using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace HCI.Nao.Gaze.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void FaceTrackingTest()
        {
            var control = new Nao.Gaze.GazeControl("192.168.1.112", 9559);
            control.start();
            Thread.Sleep(20000);
            control.end();
        }
    }
}
