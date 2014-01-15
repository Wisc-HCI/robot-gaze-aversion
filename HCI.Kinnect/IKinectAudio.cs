using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.Kinect
{
    public interface IKinectAudio
    {
        /// <summary>
        /// Start kinect but the speech recognition is not active
        /// </summary>
        void StartKinect();
        /// <summary>
        /// End Kinect Speech
        /// </summary>
        void EndKinectSpeech();
        /// <summary>
        /// Resume Kinect Audio
        /// </summary>
        void StartKinectSpeech();
        /// <summary>
        /// Prepare Kinect for sound detection
        /// </summary>
        void PrepKinectSound();
        /// <summary>
        /// Start kinect sound, the event "interupt" will fire when sound is detected
        /// </summary>
        void StartKinectSound();
        /// <summary>
        /// Stop sound recognition
        /// </summary>
        void EndKinectSound();
        /// <summary>
        /// Completlt close kinect
        /// </summary>
        void EndKinect();
        /// <summary>
        /// Restart sound sensors
        /// </summary>
        void RestartKinect();
        /// <summary>
        /// Set the grammer with dictionary
        /// must do before start detection
        /// </summary>
        /// <param name="dictionary">dictionary to react</param>
        void SetGrammer(Choices dictionary);
        /// <summary>
        /// Set grammer through XML file
        /// must do before start detection
        /// </summary>
        /// <param name="pathToXMLFile">path to XML file</param>
        void SetGrammer(string pathToXMLFile);
        /// <summary>
        /// Subscribe to a keyword.
        /// keywords are defined in the dictionary or path
        /// multiple action can be subscribe
        /// </summary>
        /// <param name="word">Keyword</param>
        /// <param name="handler">action to be done, can be a lambda example: ()=>function(var1,var2)</param>
        void Subscribe(string word, Action<string> handler);
        /// <summary>
        /// Unsubscribe all the events on the word
        /// can be change to only unsubscribe one action only
        /// </summary>
        /// <param name="word">keyword to unsubscribe</param>
        /// <param name="handler"></param>
        void Unsubscribe(string word, Action<string> handler);
        /// <summary>
        /// Delete all the subscribers
        /// </summary>
        void clearSubscribers();
        /// <summary>
        /// Return kinect sensor
        /// </summary>
        /// <returns>kinect sensor in use.</returns>
        Microsoft.Kinect.KinectSensor getSensor();
    }
}
