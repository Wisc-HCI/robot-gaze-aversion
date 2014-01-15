using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aldebaran.Proxies;
using HCI.Kinect.FaceTracking;
using System.Collections;
using MathNet.Numerics.Distributions;
using HCI.Math.Kalman;
using Basic;

namespace HCI.Nao.Gaze
{
    public class Gaze
    {
        #region Variables
        private Thread gazeThread;
        private MotionProxy motion;
        private FaceTracker tracker;
        private Basic.Bezier bezierMotion;
        private bool end;
        protected bool active;
        public bool reverse;
        public bool staticmotion = false;

        private readonly List<string> jointList;
        protected List<float> initAngle;
        protected List<float> endAngle;
        private List<float> correctLocation;
        private List<float> lastPosition;

        private readonly float NAO_BODY_YAW_OFFSET = 0.3F;
        private readonly float NAO_BODY_PITCH_OFFSET = 0F;
        private readonly float NAO_X_OFFSET = 0F;
        private readonly float NAO_Y_OFFSET = 0.4F;
        private readonly float NAO_Z_OFFSET = -0.4F;

        protected KalmanFilter kalmanFilterX;
        protected KalmanFilter kalmanFilterY;
        protected KalmanFilter kalmanFilterZ;
        protected PerlinNoise noisePitch;
        protected PerlinNoise noiseYaw;
        protected string ip;
        protected int port;

        #endregion

        public Gaze()
        {
            //Kalman Filters
            kalmanFilterX = new KalmanFilter();
            kalmanFilterX.setLast(0);
            kalmanFilterY = new KalmanFilter();
            kalmanFilterY.setLast(0);
            kalmanFilterZ = new KalmanFilter();
            kalmanFilterZ.setLast(0.4);

            //Joint list that is shared by all
            jointList = new List<string>();
            jointList.Add("HeadYaw");
            jointList.Add("HeadPitch");

            correctLocation = new List<float>();
            correctLocation.Add(0.3F);
            correctLocation.Add(0F);

            //The facetracking module
            tracker = new FaceTracker();
        }

        public Gaze(string ip, int port):this()
        {
            this.ip = ip;
            this.port = port;

            motion = new MotionProxy(ip, port);
            bezierMotion = new Bezier(ip, port);
           
        }

        #region Face Tracking Related

        /// <summary>
        /// Gives the status faceTracking
        /// </summary>
        /// <returns>True if running</returns>
        public bool FaceTrackingStatus()
        {
            return active;
        }

        /// <summary>
        /// Pause Facetracking if running
        /// Do nothing if already paused
        /// </summary>
        public void pauseFaceTracking()
        {
            //Check if the gazeThread exist.
            if (gazeThread == null)
            {
                throw new InvalidOperationException("FaceTracking not started");
            }
            active = false;
        }

        /// <summary>
        /// Resume facetracking if paused
        /// Do nothing if already running
        /// </summary>
        public void resumeFaceTracking()
        {
            //Check if the gazeThread exist.
            if (gazeThread == null)
            {
                throw new InvalidOperationException("FaceTracking not started");
            }
            active = true;
        }

        /// <summary>
        /// Start the Facetracking Thread.
        /// </summary>
        public void startFaceTracking()
        {
            if (gazeThread != null)
            {
                throw new InvalidOperationException("Face Tracking already started");
            }
            active = true;
            setStiffnesses(true);
            gazeThread = new Thread(gazeLoop);
            gazeThread.Name = ("Thread_In_Gaze");
            gazeThread.Start();
        }

        /// <summary>
        /// End Facetracking Thread
        /// </summary>
        public void endFaceTracking()
        {
            if (gazeThread == null)
            {
                throw new InvalidOperationException("Face Tracking not started");
            }
            end = true;
            gazeThread.Join();
            gazeThread = null;
            setStiffnesses(false);
        }

        /// <summary>
        /// The Loop to keep the gazeThread Alive
        /// </summary>
        private void gazeLoop()
        {
            tracker.startTracker();
            while (!end)
            {
                //Do a facetracking every 250ms, so it doesn't look so
                //weird
                Thread.Sleep(250);
                if (active)
                {
                    moveHead(tracker.getHeadLocation());
                }
            }
            tracker.endTracker();
        }

        /// <summary>
        /// command Nao to face the user.
        /// </summary>
        /// <param name="currentPosition">Position of the User from the sensor</param>
        private void moveHead(List<float> currentPosition)
        {
            //Correct the position in terms of Nao's head.
            double X = currentPosition[0] + NAO_X_OFFSET;
            double Y = currentPosition[1] + NAO_Y_OFFSET;
            double Z = currentPosition[2] + NAO_Z_OFFSET;

            //Re-calibrate the X,Y,Z using Kalman filter.
            X = kalmanFilterX.Calculate(X);
            Y = kalmanFilterY.Calculate(Y);
            Z = kalmanFilterZ.Calculate(Z);

            //change perlin noises's seed.
            noisePitch = new PerlinNoise((int)System.Math.Round(DateTime.Now.Second * 10000F));
            noiseYaw = new PerlinNoise((int)System.Math.Round(DateTime.Now.Second * 20000F));

            //Calculate the noise
            var YawNoise = noiseYaw.Noise(X, Y, Z);
            var PitchNoise = noisePitch.Noise(X, Y, Z);
            //Bring down the Noise to a more managable range.
            YawNoise *= 0.2;
            PitchNoise *= 0.15;

            var corList = new List<float>();

            //Calculate the Correct headYaw related with the offset and Noise.
            double headYaw = System.Math.Atan(currentPosition[0] / currentPosition[2]);
            headYaw -= NAO_BODY_YAW_OFFSET;
            corList.Add((float)headYaw);
            headYaw += YawNoise;

            //Just screw it up;
            if (reverse)
            {
                headYaw += 0.5;
            }
               

            //Calculate the correct headYaw related with the offset and Noise.
            double headPitch = -1 * (System.Math.Atan(currentPosition[1] / currentPosition[2]));
            headPitch -= NAO_BODY_PITCH_OFFSET;
            corList.Add((float)(headPitch - 0.2));
            headPitch += PitchNoise;

            headPitch -= 0.2;

            correctLocation = corList;

            //Save the current position as the last position.
            var lastPosition = new List<float>();
            lastPosition.Add((float)headYaw);
            lastPosition.Add((float)headPitch);
            this.lastPosition = lastPosition;

            //Set the angles to the new angle.
            motion.setAngles(jointList, lastPosition, 0.04F);
        }

        #endregion

        #region Gaze Related

        /// <summary>
        /// Start Head Gaze
        /// </summary>
        /// <param name="angleList">Angle to move in a list</param>
        /// <param name="absolute">Absolute Angle</param>
        /// <returns>The Current Angle</returns>
        public List<float> startGaze(List<float> angleList,bool absolute=false)
        {
            return startGaze(angleList[0], angleList[1],absolute);
        }

        /// <summary>
        /// Start HeadGaze
        /// </summary>
        /// <param name="yaw">Angle for yaw</param>
        /// <param name="pitch">Angle for pitch</param>
        /// <param name="absolute">If the angle is absolute or not</param>
        /// <returns></returns>
        public List<float> startGaze(float yaw,float pitch,bool absolute=false)
        {
            //Deactivate FaceTracking
            active = false;

            //List for final angle
            endAngle = new List<float>();
            endAngle.Add(yaw); 
            endAngle.Add(pitch);

            //reverse is true, means look at him
            if (reverse)
            {
                endAngle = correctLocation;
                absolute = true;
            }

            //get the current degree, if its in a middle of a gaze
            //changue to last position.
            if (lastPosition != null)
            {
                initAngle = lastPosition;
            }
            else
            {
                initAngle = motion.getAngles(this.jointList, true);
            }

            //When it should not move at all... wakakakaka
            if (staticmotion)
            {
                return initAngle;
            }

            //Change the final angle to be relative to the currentAngle
            if (!absolute)
            {
                endAngle[0] += initAngle[0];
                endAngle[1] += initAngle[1];
            }

            bezierMotion = new Bezier(ip, port);
            bezierMotion.beziermove(jointList, 1, initAngle, endAngle);
            return initAngle;
        }

        /// <summary>
        /// Return Gaze
        /// </summary>
        /// <param name="finalAngle">The final angle</param>
        public void returnGaze(List<float> finalAngle)
        {
            //If wrong motion is correct
            //who cares where it ends up to.
            if(!reverse)
            initAngle = finalAngle;
            returnGaze();
        }

        /// <summary>
        /// Return Gaze
        /// </summary>
        public void returnGaze()
        {
            if (staticmotion)
            {
                return;
            }
            //Get the current position.
            //Reinitialize the motion proxy to prevent SOAP error.
            motion = new MotionProxy(ip, port);
            List<float> curlist = motion.getAngles(this.jointList, false);

            //The final position
            var list = new List<float>();
            list.Add(initAngle[0]);
            list.Add(initAngle[1]);

            //Initialize to prevent SOAP error.
            bezierMotion = new Bezier(ip, port);
            bezierMotion.beziermove(jointList, 1, curlist, list);
            
            //Deactive facetracking for side effect - 08/06/2013
            //active = true;
        }

        #endregion

        /// <summary>
        /// Set the stiffness of all the joint
        /// </summary>
        /// <param name="option">true to set, false to unset</param>
        public void setStiffnesses(bool option)
        {
            if (option)
            {
                ArrayList stiff = new ArrayList();
                stiff.Add(1.0F);
                stiff.Add(1.0F);
                motion.setStiffnesses(jointList, stiff);
            }
            else
            {
                ArrayList stiff = new ArrayList();
                stiff.Add(0F);
                stiff.Add(0F);
                motion.setStiffnesses(jointList, stiff);
            }
        }

    }
}
