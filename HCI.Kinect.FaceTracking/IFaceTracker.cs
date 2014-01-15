using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.Kinect.FaceTracking
{
    interface IFaceTracker
    {
        /// <summary>
        /// Start Face Tracking
        /// </summary>
        void startTracker();

        /// <summary>
        /// Return the current face location,
        /// this is relative to the Kinect.
        /// </summary>
        /// <returns>List of (X_cordinate, Y_coordinate, Z_coordinate)</returns>
        List<float> getHeadLocation();

        /// <summary>
        /// End Face Tracking
        /// </summary>
        void endTracker();

        /// <summary>
        /// Check if the tracker is running
        /// </summary>
        /// <returns>True if tracker is running</returns>
        bool trackerStatus();
    }
}
