using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Threading;
using System.Collections;
using HCI.Math.Kalman;

namespace HCI.Kinect.FaceTracking
{
    public class FaceTracker : IFaceTracker
    {
        #region Variables

        public Thread thread;
        private KinectSensor kinect = null;
        private Skeleton[] skeletonData = new Skeleton[0];
        private bool Flag;
        private bool isRunning;
        protected List<float> currentAngle;

        #endregion

        //Pass the sensor from the kinect audio or create own sensor
        //Just copy the sensor code from the audio part
        public FaceTracker(KinectSensor sensor)
        {

            Flag = false;
            kinect = sensor;
            kinect.SkeletonStream.Enable(); // Enable skeletal tracking
        }

        public FaceTracker()
        {

            Flag = false;
            //Get a kinect sensor
            prepKinect();
            kinect.SkeletonStream.Enable(); // Enable skeletal tracking
            currentAngle = new List<float>();
            kinect.Start();
        }

        #region Public Functions

        public void startTracker()
        {
            isRunning = true;
            Flag = false;
            thread = new Thread(StartKinectST);
            thread.Name = "FaceTrackerThread";
            thread.Start();
        }

        public bool trackerStatus()
        {
            return isRunning;
        }

        public void endTracker()
        {
            Flag = true;
            thread.Join();
            thread = null;
            isRunning = false;
        }

        public List<float> getHeadLocation()
        {
            //Move the currentAngle to a new list to prevent threading to overwrite it
            List<float> list = currentAngle;
            if (list.Count == 3)
            {
                return list;
            }
            else
            {
                //If the kinect have never picked up a face, return a default value
                return new List<float> { 0F, -0.2F, 0.8F };
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Prepare the kinect by finding a available sensor
        /// </summary>
        private void prepKinect()
        {
            kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
        }

        /// <summary>
        /// The kinect's loop to let the thread stay alive
        /// </summary>
        private void StartKinectST()
        {
            Console.WriteLine("Start Tracking thread");
            skeletonData = new Skeleton[kinect.SkeletonStream.FrameSkeletonArrayLength]; // Allocate ST data

            if (this.kinect != null && this.kinect.DepthStream != null && this.kinect.SkeletonStream != null)
            {
                this.kinect.DepthStream.Range = DepthRange.Near; // Depth in near range enabled
                this.kinect.SkeletonStream.EnableTrackingInNearRange = true; // enable returning skeletons while depth is in Near Range
                this.kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated; // Use seated tracking
            }

            //Start the event
            kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady); 
            while (!Flag)
            {
                Thread.Sleep(100);
            }
            kinect.SkeletonFrameReady -= new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
            return;
        }

        /// <summary>
        /// Event handler when the frame is ready
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //Unsubscribe to prevent multiple events. <- not sure if needed.
            kinect.SkeletonFrameReady -= new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) // Open the Skeleton frame
            {

                if (skeletonFrame != null && this.skeletonData != null) // check that a frame is available
                {
                    skeletonData = new Skeleton[kinect.SkeletonStream.FrameSkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(this.skeletonData); // get the skeletal information in this frame
                    readHeadPosition();
                    skeletonData = new Skeleton[0];
                }
            }
            kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
        }

        /// <summary>
        /// Store the newest head's position in the currlist
        /// </summary>
        private void readHeadPosition()
        {
            foreach (Skeleton sk in this.skeletonData)
            {
                if (sk != null)
                {
                    // TODO: Implemented it to tracked the same person. 

                    //check if the skeleton is tracked
                    if (sk.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        JointCollection collection = sk.Joints;
                        Joint head = collection[JointType.Head];
                        List<float> newList = new List<float>();
                        newList.Add(head.Position.X);
                        newList.Add(head.Position.Y);
                        newList.Add(head.Position.Z);
                        currentAngle = newList;
                    }
                }
            }
        }
        #endregion
    }
}
