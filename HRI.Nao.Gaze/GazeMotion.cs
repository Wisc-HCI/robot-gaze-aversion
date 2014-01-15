using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aldebaran.Proxies;
using MathNet.Numerics.Distributions;

namespace HCI.Nao.Gaze
{
    /// <summary>
    /// The main gaze loop, abstract layer between facetracking and gazes
    /// All gaze are technically controlled from this point.
    /// </summary>
    public class GazeMotion
    {
        #region Variables
        //Thread to hold the motion
        private Thread gazeLoop;
        private bool _reverse;
        private bool _static;
        public bool reverse
        {
            set
            {
                _reverse = value;
                if(gazemotion!= null)
                    gazemotion.reverse = value;
            }
            get
            {
                return _reverse;
            }
        }

        public bool staticMotion
        {
            set
            {
                _static = value;
                if (gazemotion != null)
                    gazemotion.staticmotion = value;
            }
            get
            {
                return _static;
            }
        }

        //mood for the current gaze
        // speaking, listening, nothing
        private gazeMood _mode;
        public gazeMood mode {
            get
            {
                return _mode;        
            }
            set
            {
                //If mood is change, update all variable
                _mode = value;
                lastIntimacy = DateTime.Now;
                speakingIntimacy = false;
                //Signal a mood change
                MoodChanged = true;
            }
        }
        //Type of gaze going to be performed
        public gazeType type {get;set;}
        //The gaze class.
        private Gaze gazemotion;

        private bool endFlag;
        private bool MoodChanged;
        private bool _speakingIntimacy;
        public bool speakingIntimacy{
            get
            {
                return _speakingIntimacy;
            }
            set
            {
                _speakingIntimacy = value;
                if (value)
                {
                    type = gazeType.intimacy;
                }
                lastIntimacy = DateTime.Now;
            }
            
        } 

        //Distribution between Intimacy Gazes
        private Normal listeningIntimacyDistance;
        private Normal listeningIntimacyPause;
        private Normal speakingIntimacyDistance;

        //Lead time before the phrase
        protected DateTime starttime; //to remember the starting time.
        protected DateTime leadtime; //when the gaze should start
        protected DateTime pausetime; //how long the gaze have to pause
        protected TimeSpan elapsedtime; //the amount of time since the start
        protected DateTime endtime; //Temporary nothing usefull
        protected DateTime intimacyStoptime; //when to stop intimacy
        private DateTime lastIntimacy; //When is the last intimacy gaze

        //What is the moving variables.
        private float END_HEAD_UP = -0.3F; //Moving up looks weird
        private float END_HEAD_DOWN = 0.4F;
        private float END_HEAD_SIDE = 0.5F;
        private float INTIMACY_MULTIPLIYER = 0.4F;
        private float SIDE_MULTIPLIER = 0.7F;

        //Store the angle before intimacy gaze
        //To write the default return location of the gaze
        //if the gaze is called in between an intimacy gaze
        private List<float> preIntimacyAngle;

        private string ip;
        private int port;

        #endregion

        public GazeMotion(string ip, int port):this()
        {
            this.ip = ip;
            this.port = port;
            gazemotion = new Gaze(ip, port);
            mode = gazeMood.listening;
            type = gazeType.intimacy;
        }

        public GazeMotion()
        {
            endFlag = false;
            staticMotion = false;
            speakingIntimacy = false;
            initializeVariables();
        }

        /// <summary>
        /// Initialize Normal Variables
        /// </summary>
        private void initializeVariables()
        {
            //Intimacy related
            listeningIntimacyDistance = new Normal(4, 0.5);
            listeningIntimacyPause = new Normal(0.5, 0.1);
            speakingIntimacyDistance = new Normal(5, 0.5);
        }

        /// <summary>
        /// Start gazes
        /// automatically start face tracking
        /// </summary>
        public void start()
        {
            if (gazeLoop != null)
            {
                throw new InvalidOperationException("Already Started");
            }
            gazeLoop = new Thread(loopThread);
            gazeLoop.Name = ("Main Loop");
            gazeLoop.IsBackground = false;
            lastIntimacy = DateTime.Now;
            gazeLoop.Start();
        }

        /// <summary>
        /// End gazes
        /// </summary>
        public void end()
        {
            if (gazeLoop == null)
            {
                return; //It already ended
            }
            endFlag = true;
            gazeLoop.Join();
        }

        /// <summary>
        /// The action to be looped over
        /// </summary>
        private void loopThread()
        {
            gazemotion.startFaceTracking();
            while (!endFlag)
            {
                //Update the gaze every 0.1 seconds
                Thread.Sleep(100);
                update();
            }
            gazemotion.endFaceTracking();
        }

        public void lookingMode()
        {
            mode = gazeMood.looking;
        }

        /// <summary>
        /// Check which gaze it should go next.
        /// </summary>
        private void update()
        {
            //In listening mood.
            if (Enum.Equals(mode, gazeMood.listening))
            {
                MoodChanged = false;
                //if no motion should be running at all....
                if (!staticMotion)
                {
                    //Check if the time is larger than the intimacy distance value.
                    var time = (DateTime.Now - lastIntimacy).TotalSeconds;
                    if (time > listeningIntimacyDistance.Sample())
                    {
                        //Run an intimacy gaze
                        preIntimacyAngle = intimacyGaze();
                    }
                }
            }
            //In speaking mood
            else if(Enum.Equals(mode, gazeMood.speaking))
            {
                MoodChanged = false;
                //check if an intimacy gaze should be runed
                if (speakingIntimacy)
                {
                    //There is no intimacy without facetracking
                    if (!gazemotion.FaceTrackingStatus())
                    {
                        gazemotion.resumeFaceTracking();
                    }
                    if (!staticMotion)
                    {
                        var time = (DateTime.Now - lastIntimacy).TotalSeconds;
                        if (time > speakingIntimacyDistance.Sample())
                        {
                            intimacyGaze();
                        }
                    }
                }
                else
                {
                    //Just loop through this, gazes are order directly to
                    //the Gaze class
                }
            }
            //A mood where the nao does nothing
            //Not even facetracking
            // TODO: Ask Sean should nao do face tracking
            else if (Enum.Equals(mode, gazeMood.nothing))
            {
                MoodChanged = false;
                //Close facetracking if the facetracking is running
                if (gazemotion.FaceTrackingStatus())
                {
                    gazemotion.pauseFaceTracking();
                }
            }
            else if (Enum.Equals(mode, gazeMood.looking))
            {
                MoodChanged = false;
                //Close facetracking if the facetracking is running
                if (!gazemotion.FaceTrackingStatus())
                {
                    gazemotion.resumeFaceTracking();
                }
            }
        }

        /// <summary>
        /// Helper function to go to listening mood
        /// </summary>
        public void listeningMood()
        {
            this.type = gazeType.intimacy;
            this.mode = gazeMood.listening;
            speakingIntimacy = false;
            //Manual restart facetracking
            this.gazemotion.resumeFaceTracking();
        }

        #region Gazes

        /// <summary>
        /// Abstract layer for the a gaze
        /// </summary>
        /// <param name="type">Type of gaze</param>
        public void startGaze(gazeType type)
        {
            this.type = type;
            startGaze();
        }

        /// <summary>
        /// Abstract layer for the a gaze
        /// </summary>
        public void startGaze()
        {

            this.mode = GazeMotion.gazeMood.speaking;
            gazemotion.startGaze(DirectionChance(this.type));           
        }

        /// <summary>
        /// Abstract Layer for the return gaze
        /// </summary>
        public void returnGaze()
        {
            if (preIntimacyAngle != null)
                gazemotion.returnGaze(preIntimacyAngle);
            else
                gazemotion.returnGaze();
        }

        /// <summary>
        /// Intimacy Gaze that is called sometimes
        /// </summary>
        /// <returns>A list of initial angle if the gaze is interupted</returns>
        private List<float> intimacyGaze()
        {
            double pause;
            if (Enum.Equals(mode, gazeMood.listening))
            {
                pause = listeningIntimacyPause.Sample();
            }
            else
            {
                pause = listeningIntimacyPause.Sample();
            }
            List<float> initialAngle = gazemotion.startGaze(DirectionChance(this.type));
            for (int i = 0; i < (pause * 10); i++)
            {
                Thread.Sleep(100);
                if (endFlag)
                    return initialAngle;
                if (!speakingIntimacy && Enum.Equals(this.mode, gazeMood.nothing))
                    break;
                if (MoodChanged)
                    return initialAngle;
            }
            gazemotion.returnGaze();

            //Manually resume facetracking if these condition is met
            //This is to prevent gaps betweenth
            if (Enum.Equals(this.mode, gazeMood.listening))
                gazemotion.resumeFaceTracking();
            else if (Enum.Equals(this.mode, gazeMood.speaking) && speakingIntimacy)
            {
                gazemotion.resumeFaceTracking();
            }
            lastIntimacy = DateTime.Now;
            return null;
        }

        /// <summary>
        /// Figure out the direction according to change
        /// </summary>
        /// <param name="type">Type of Gaze</param>
        /// <returns>Direction and Distance of the movement</returns>
        protected List<float> DirectionChance(gazeType type)
        {
            Random rnd = new Random();
            int random1 = rnd.Next(0, 10); //Min is 0, Max is 9;
            int random2 = rnd.Next(1, 3); // just 1 or 2. //50-50
            List<float> returnList = new List<float>();

            if (Enum.Equals(type,gazeType.intimacy)) //Intimicy
            {
                if (random1 < 7) // 0-6 70%
                {
                    //Go to the side.
                    returnList.Add((END_HEAD_SIDE * INTIMACY_MULTIPLIYER));
                    returnList.Add(0F);
                }
                else if (random1 < 8) //8 10%
                {
                    //Gaze up
                    if (random2 == 1)
                    {
                        //Up and side
                        returnList.Add(END_HEAD_SIDE * INTIMACY_MULTIPLIYER);
                        returnList.Add(END_HEAD_UP * INTIMACY_MULTIPLIYER);
                    }
                    else
                    {
                        //just up
                        returnList.Add(0F);
                        returnList.Add(END_HEAD_UP * INTIMACY_MULTIPLIYER);
                    }
                }
                else
                {
                    //Gaze down
                    if (random2 == 1) //20%
                    {
                        //down and side
                        returnList.Add(END_HEAD_SIDE * INTIMACY_MULTIPLIYER);
                        returnList.Add(END_HEAD_DOWN * INTIMACY_MULTIPLIYER);
                    }
                    else
                    {
                        //down only
                        returnList.Add(0F);
                        returnList.Add(END_HEAD_DOWN * INTIMACY_MULTIPLIYER);
                    }
                }
            }
            else
            {
                if (random1 < 1) //10%
                {
                    //just to the side
                    returnList.Add(END_HEAD_SIDE);
                    returnList.Add(0F);
                }
                else if (random1 < 8) // 70%
                {
                    if (random2 == 1)
                    {
                        //up and side
                        returnList.Add(END_HEAD_SIDE * SIDE_MULTIPLIER);
                        returnList.Add(END_HEAD_UP);
                    }
                    else
                    {
                        //just up
                        returnList.Add(0F);
                        returnList.Add(END_HEAD_UP);
                    }
                }
                else
                {
                    if (random2 == 1)
                    {
                        //down and side
                        returnList.Add(END_HEAD_SIDE * SIDE_MULTIPLIER);
                        returnList.Add(END_HEAD_DOWN);
                    }
                    else
                    {
                        //just down;
                        returnList.Add(0F);
                        returnList.Add(END_HEAD_DOWN);
                    }
                }
            }
            return returnList;
        }
        #endregion

        #region Enum Variables
        public enum gazeMood
        {
            listening,
            speaking,
            nothing,
            looking
        }

        public enum gazeType
        {
            intimacy,
            turntaking,
            thinking,
            cognitize
        }
        #endregion
    }
}