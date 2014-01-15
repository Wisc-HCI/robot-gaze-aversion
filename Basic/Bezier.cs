using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aldebaran.Proxies;
using System.Collections;

namespace Basic
{
    public class Bezier
    {
        private string ip;
        private int port;
        private MotionProxy motion;
        public Bezier(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            motion = new MotionProxy(this.ip, this.port);
        }

        public void beziermove(List<string> joint, float maxtime, List<float>startAngle, List<float> endAngle)
        {
            if (joint.Count != endAngle.Count || joint.Count != startAngle.Count)
                throw new IndexOutOfRangeException();

            List<string> jointList = joint;

            ArrayList timeList = new ArrayList();
            for (int i = 0; i < joint.Count; i++)
            {
                ArrayList time = new ArrayList();
                time.Add(0.01F);
                time.Add(maxtime);
                timeList.Add(time);
            }

            ArrayList controlPointList = new ArrayList();
            for (int i = 0; i < joint.Count; i++)
            {
                ArrayList cp1 = new ArrayList();
                cp1.Add(startAngle[i]);
                ArrayList Nullhandle = new ArrayList();
                Nullhandle.Add(2);
                Nullhandle.Add(0F);
                Nullhandle.Add(0F);
                ArrayList handle1 = new ArrayList();
                handle1.Add(2);
                handle1.Add((maxtime/2F));
                handle1.Add(0F);
                cp1.Add(Nullhandle);
                cp1.Add(handle1);
                ArrayList cp2 = new ArrayList();
                cp2.Add(endAngle[i]);
                ArrayList handle2 = new ArrayList();
                handle2.Add(2);
                handle2.Add(-1*(maxtime / 2F));
                handle2.Add(0F);
                cp2.Add(handle2);
                cp2.Add(Nullhandle);
                ArrayList jointcontrollist = new ArrayList();
                jointcontrollist.Add(cp1);
                jointcontrollist.Add(cp2);
                controlPointList.Add(jointcontrollist);
            }
            ArrayList stiff = new ArrayList();
            for (int i = 0; i < jointList.Count; i++)
            {
                stiff.Add((float)1.0);
            }
            motion.setStiffnesses(jointList,stiff);
            motion.angleInterpolationBezier(jointList, timeList, controlPointList);
        }
    }
}
