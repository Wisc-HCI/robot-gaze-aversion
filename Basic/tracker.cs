using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aldebaran.Proxies;
using System.Threading;
using System.Collections;

namespace Basic
{
    public class tracker
    {
        private string ip;
        private int port;
        FaceTrackerProxy track;
        MotionProxy motion;
        
        public tracker(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            motion = new MotionProxy(ip, port);
            RobotPostureProxy pos = new RobotPostureProxy(ip, port);
            ArrayList stiff = new ArrayList();
            stiff.Add(1.0F);
            stiff.Add(1.0F);
            ArrayList joint = new ArrayList();
            joint.Add("Head");
            motion.setStiffnesses(joint, stiff);
            pos.goToPosture("SitRelax", 0.6F);
            track = new FaceTrackerProxy(ip, port);
            track.setWholeBodyOn(false);
            track.startTracker();
            Thread.Sleep(30000);
            track.stopTracker();
            stiff.Clear();
            stiff.Add(0.0F);
            stiff.Add(0.0F);
            motion.setStiffnesses(joint, stiff);
        }
    }
}
