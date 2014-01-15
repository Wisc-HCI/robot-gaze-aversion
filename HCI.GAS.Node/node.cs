using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basic;
using System.Threading;
using MathNet.Numerics.Distributions;
using Aldebaran.Proxies;

namespace HCI.GAS.Node
{
    public class Node : INode
    {
        #region Variables
      
        private string ip;
        private int port;
        private Thread thread;
        private bool end;
        private int type; 
        private Normal nextIntimacyDistributionSpeaking = null;
        private Normal nextIntimacyDistributionListening = null;
        private Normal intimacyLengthDistribution = null;
        private FaceTrackerProxy tracker;

        #endregion

        public Node(string ip, int port,int type)
        {
            this.type = type;
            this.ip = ip;
            this.port = port;
            thread = new Thread(new ThreadStart(beginNode));
            end = false;
            nextIntimacyDistributionSpeaking = new Normal(4.0, 1.0);
            nextIntimacyDistributionListening = new Normal(5.0, 1.0);
            intimacyLengthDistribution = new Normal(1.0, 0.2);
            tracker = new FaceTrackerProxy(ip,port);
            tracker.setWholeBodyOn(false);
        }

        #region Implementation

        public void stopNode()
        {
            thread.Abort();
            thread.Join();
            thread = null;
        }
        public void startNode()
        {
            if (!thread.IsAlive)
            {
                thread.Start();
            }
            else
                Console.WriteLine("Thread already started");
        }

        #endregion

        #region helper methods

        protected void beginNode()
        {
            Random rnd = new Random();
            Bezier move = new Bezier(this.ip, this.port);
            Console.WriteLine("Start noding");
            List<String> jointlist = new List<string>();
            jointlist.Add("HeadPitch");
            List<float> startlist = new List<float>();
            List<float> endlist = new List<float>();
            if (type == 3)
            {
                startlist.Add(0F);
                endlist.Add(0.3F);
                move.beziermove(jointlist, 0.50F, startlist, endlist);
            }

            while (!end)
            {
                if(type == 2)
                    tracker.startTracker();
                endlist.Clear();
                startlist.Clear();
                double wait = 0;
                double pause = intimacyLengthDistribution.Sample();
                pause *= 1000;
                int pause_int = (int)pause;
                if (type == 0)//speaking // 2 is wrong motion.
                {
                    wait = nextIntimacyDistributionSpeaking.Sample();
                }
                else //listening
                {
                    wait = nextIntimacyDistributionListening.Sample();
                }
                wait *= 1000;
                int wait_int = (int)wait;
                System.Threading.Thread.Sleep(wait_int);
                if (type == 2)
                {
                    try
                    {
                        tracker.stopTracker();
                    }
                    catch (Exception)
                    {
                        end = true;
                    }
                }
                if (type == 3)
                {
                    startlist.Add(0F);
                    endlist.Add(0F);
                    move.beziermove(jointlist, 0.50F, startlist, endlist);
                    endlist.Clear();
                    endlist.Add(0.3F);
                    System.Threading.Thread.Sleep(pause_int);
                    move.beziermove(jointlist, 0.50F, startlist, endlist);
                }
                else
                {
                    startlist.Add(0F);
                    endlist.Add((float)0.2);
                    move.beziermove(jointlist, 0.50F, startlist, endlist);
                    endlist.Clear();
                    endlist.Add(0F);
                    System.Threading.Thread.Sleep(pause_int);
                    move.beziermove(jointlist, 0.50F, startlist, endlist);
                }
            }
        }

        #endregion
    }
}
