using Aldebaran.Proxies;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HCI.Nao.Gaze
{
    public class GazeControl
    {
        #region Variables

        protected GazeMotion gazemotions;

        //Different Distributions
        private Normal cogStartDistributionAnswer = null;
        private Normal cogEndDistribution = null;
        private Normal turnStartDistribution = null;
        private Normal turnStopDistribution = null;
        private Normal turnBackDistribution = null;
        private Normal utteranceEndTime = null;

        protected DateTime starttime; //to remember the starting time.
        protected DateTime leadtime; //when the gaze should start
        protected DateTime pausetime; //how long the gaze have to pause
        protected TimeSpan elapsedtime; //the amount of time since the start

        //Use to play audio files
        private AudioPlayerProxy player;
        //The id of the following files
        private int phrase1ID;
        private int phrase2ID;

        //private double SPEAKING_INTIMACY_END_TIME = 1.05;

        #endregion

        #region Intializers

        public GazeControl()
        {
            initializeVariables();
        }

        public GazeControl(string ip, int port):this()
        {
            gazemotions = new GazeMotion(ip, port);
            player = new AudioPlayerProxy(ip, port);
            gazemotions.type = GazeMotion.gazeType.intimacy;
            gazemotions.mode = GazeMotion.gazeMood.listening;
        }

        /// <summary>
        /// Use to intialize multiple Normal Varialbes
        /// </summary>
        public void initializeVariables()
        {
            //Cognitive
            //this is the time for cognitive motion before it talks
            cogStartDistributionAnswer = new Normal(2.5, 0.47);//Mean,SD
            //this is the time for cognitive motion after it talks
            cogEndDistribution = new Normal(1, 0.63);//Mean,SD

            //TurnTaking
            //How much time before turning the head
            turnStartDistribution = new Normal(1.03, 0.39);
            //When to start turning.
            turnStopDistribution = new Normal(1.27, 0.5);
            //amount of time wait untill the gaze come back.

            //No longer used~
            turnBackDistribution = new Normal(2.41, 0.56);

            utteranceEndTime = new Normal(2.41, 0.56);
        }

        #endregion

        #region High Level Controls

        /// <summary>
        /// Starts the Intimacy Gaze and FaceTracking
        /// </summary>
        public void start()
        {
            gazemotions.start();
        }

        /// <summary>
        /// Pause the Intimacy Gaze and FaceTracking
        /// </summary>
        public void pauseIntimacy()
        {
            gazemotions.mode = GazeMotion.gazeMood.nothing;
        }

        /// <summary>
        /// resume intimacy and facetracking
        /// </summary>
        public void resumeIntimacy()
        {
            gazemotions.listeningMood();
        }

        /// <summary>
        /// End the Intimacy Gaze and FaceTracking
        /// WARNING: this will break the audio of kinect if start again
        /// </summary>
        public void end()
        {
            gazemotions.end();
        }

        //Depreciated
        public void reverseControls()
        {
            //Change to inverse mood for Intimacy and FaceTracking
            gazemotions.reverse = true;
        }

        //Depreciated
        public void StaticControls()
        {
            gazemotions.staticMotion = true;
        }

        public void gazeControlConfiguration(int condition)
        {
            switch (condition)
            {
                case 1:
                    gazemotions.staticMotion = true;
                    break;
                case 3:
                    gazemotions.reverse = true;
                    break;
            }
        }

        #endregion 

        #region Selectors

        public void startGaze(int type)
        {
            switch(type)
            {
                case 1:
                    thinkingGazeStart();
                    break;
                case 2:
                    cognitizeGaze();
                    break;
                case 3:
                    disclosureGaze();
                    break;
                case 4:
                    turnTakingGazeStart();
                    break;
            }
        }

        public void returnGaze(int type,bool talk)
        {

            switch (type)
            {
                case 1:
                    thinkingGazeReturn();
                    break;
                case 2:
                    //They dont have returnGaze...
                    break;
                case 3:
                    break;
                case 4:
                    turnTakingGazeEnd(talk);
                    break;
            }
        }

        #endregion

        #region Thinking

        protected void thinkingGazeStart()
        {
            gazemotions.type = GazeMotion.gazeType.thinking;
            gazemotions.startGaze();
        }

        public void thinkingGazeReturn()
        {
            int id = player.post.play(this.phrase1ID);
            gazemotions.returnGaze();
            speakingIntimacy(this.phrase1ID);
            player.wait(id, 0);
            gazemotions.listeningMood();
        }

        #endregion

        #region Cognitize Gaze

        public void cognitizeGaze()
        {
            double motionStartTime = cogStartDistributionAnswer.Sample();
            double motionEndTime = cogEndDistribution.Sample() + motionStartTime;
            var utteranceTime = utteranceEndTime.Sample();

            this.starttime = DateTime.Now;
            timeUpdate();

            gazemotions.startGaze(GazeMotion.gazeType.cognitize);

            while (elapsedtime.TotalSeconds < motionStartTime)
            {
                timeUpdate();
                Thread.Sleep(20);
            }

            float totalTime = player.getFileLength(phrase1ID);
            int id = player.post.play(this.phrase1ID);
            timeUpdate();
            while (elapsedtime.TotalSeconds < motionEndTime)
            {
                timeUpdate();
                //Return from this function because there is no time left for intimacy gaze.
                if ((totalTime - player.getCurrentPosition(phrase1ID)) < utteranceTime)
                {
                    gazemotions.returnGaze();
                    gazemotions.lookingMode();
                    player.wait(id, 0);
                    gazemotions.listeningMood();
                    return;
                }
            }
            gazemotions.returnGaze();
            speakingIntimacy(phrase1ID,utteranceTime);
            player.wait(id, 0);
            //Go back to listeningMood
            gazemotions.listeningMood();
        }
        #endregion

        #region Disclosures

        protected void disclosureGaze()
        {
            int id = player.post.play(this.phrase1ID);
            speakingIntimacy(phrase1ID);
            player.wait(id, 0);
            gazemotions.listeningMood();
        }

        #endregion

        #region TurnTaking Gaze

        protected void turnTakingGazeStart()
        {
            gazemotions.type = GazeMotion.gazeType.turntaking;
            //New Addition:: it was still having listening intimacy the last time
            gazemotions.mode = GazeMotion.gazeMood.speaking;
            this.starttime = DateTime.Now;
            double timebeforeTalkEnd = turnStartDistribution.Sample();
            double motionStartTime = (double)player.getFileLength(phrase1ID) - timebeforeTalkEnd;
            float totalTime = player.getFileLength(phrase1ID);
            int id = player.post.play(phrase1ID);
            timeUpdate();
            while (elapsedtime.TotalSeconds < motionStartTime)
            {
                //Depreciated due to it always firing
                //if (totalTime - player.getCurrentPosition(phrase1ID) < 1.05)
                //{
                  //  break;
                //}
                timeUpdate();
            }
            gazemotions.startGaze();
            player.wait(id, 0);
        }

        protected void turnTakingGazeEnd(bool talk)
        {
         
            if (!talk)
            {
                gazemotions.returnGaze();
                gazemotions.listeningMood();
            }
            else
            {
                this.starttime = DateTime.Now;
                double motionStartTime = cogEndDistribution.Sample();
                float totalTime = player.getFileLength(phrase2ID);
                int id = player.post.play(phrase2ID);
                timeUpdate();
                while (elapsedtime.TotalSeconds < motionStartTime)
                {
                    timeUpdate();
                    //This is unlikely to happen
                    if ((totalTime - player.getCurrentPosition(phrase2ID)) < 1.05)
                        break;
                    Thread.Sleep(20);
                }
                gazemotions.returnGaze();
                speakingIntimacy(phrase2ID);
                player.wait(id, 0);
                gazemotions.listeningMood();
            }
        }

        #endregion

        private void timeUpdate()
        {
            elapsedtime = DateTime.Now.Subtract(starttime);
        }

        /// <summary>
        /// Helper function to control the ending of speaking intimacy
        /// stops the intimacy 1.05 second before end of speech
        /// </summary>
        /// <param name="phraseID"></param>
        protected void speakingIntimacy(int phraseID)
        {
            float totalTime = player.getFileLength(phraseID);
            //Start Intimacy Gazes.
            gazemotions.speakingIntimacy = true;
            double utteranceTime = utteranceEndTime.Sample();
            //Intimacy will stop 1.05 seconds before the end of speech
            while ((totalTime - player.getCurrentPosition(phraseID)) > 
                utteranceTime)
            {
                Thread.Sleep(20);
            }
            //dactivate speaking gaze
            gazemotions.speakingIntimacy = false;
            //go to total still
            gazemotions.lookingMode();
        }

        protected void speakingIntimacy(int phraseID,double time)
        {
            float totalTime = player.getFileLength(phraseID);
            //Start Intimacy Gazes.
            gazemotions.speakingIntimacy = true;
            while ((totalTime - player.getCurrentPosition(phraseID)) >
                time)
            {
                Thread.Sleep(20);
            }
            //dactivate speaking gaze
            gazemotions.speakingIntimacy = false;
            //go to total still
            gazemotions.lookingMode();
        }

        #region Audio Playing Related

        /// <summary>
        /// Set the path to the first audio file
        /// </summary>
        /// <param name="path1">Path to first audio file</param>
        public void setPath(string path1)
        {
            this.phrase1ID = player.loadFile(path1);
        }

        /// <summary>
        /// Set path to the audio files
        /// </summary>
        /// <param name="path1">Path 1</param>
        /// <param name="path2">Path 2</param>
        public void setPath(string path1, string path2)
        {
            this.phrase2ID = player.loadFile(path2);
            setPath(path1);
        }

        /// <summary>
        /// Play the first phrase without any gazes
        /// INTIMACY GAZE STILL RUNS
        /// </summary>
        public void playPhrase1()
        {
            int id = player.post.play(phrase1ID);
            speakingIntimacy(phrase1ID);
            player.wait(id, 0);
        }

        /// <summary>
        /// Play the second phrase without any gazes
        /// WARNING: INTIMACY GAZE STILL RUNS
        /// </summary>
        public void playPhrase2()
        {
            int id = player.post.play(phrase2ID);
            speakingIntimacy(phrase2ID);
            player.wait(id, 0);
        }

        #endregion
    }

}
