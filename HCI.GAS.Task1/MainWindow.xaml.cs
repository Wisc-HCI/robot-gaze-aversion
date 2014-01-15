using Aldebaran.Proxies;
using Basic;
using HCI.GAS.Logs;
using HCI.GAS.Kinect;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HCI.Nao.Gaze;

namespace HCI.GAS.Task1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables

        private string ip;
        private int port;
        private int condition;
        private bool started;

        private MotionProxy motion;
        private TextToSpeechProxy tts;

        private GazeControl gazeControl;
        private LedsProxy led;
        private KinectAudio kinect;
        private Bezier bezierMotion;
        private bool end;
        private bool interupt;
        private bool repeat;
        private bool happened;

        private Log logfile;
        private DateTime startTime;
        private DateTime InteruptedTime;
        private DateTime endTime;

        private Check.Check completeCheck;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.Background = new SolidColorBrush(Colors.LightGreen);
        }

        public MainWindow(string ip,int port,int condition):this()
        {
            this.ip = ip;
            this.port = port;
            this.condition = condition;
            this.started = false;
            led = new LedsProxy(ip, port);
            led.off("ChestLeds");
            led.on("ChestLedsGreen");
            completeCheck = new Check.Check();
        }

        #region GUI Related Codes
        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!started)
            {
                led.off("ChestLeds");
                started = true;
                task();
            }
            else
            {
                Console.WriteLine("Already started");
                return;
            }
        }

        private void endWindow()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.Close();
            }));

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            end = true;
            if(gazeControl != null)
                gazeControl.end();
            if (led != null)
            {
                led.off("ChestLeds");
                led.on("ChestLedsRed");
            }
            if(kinect != null)
                kinect.EndKinect();
            if(logfile != null)
            logfile.End();
        }
        #endregion

        #region Task 1 related Codes
        private Thread taskThread = null;
        public void task()
        {
            this.Background = new SolidColorBrush(Colors.LightBlue);
            motion = new MotionProxy(ip, port);
            tts = new TextToSpeechProxy(ip, port);
            bezierMotion = new Bezier(ip, port);
            string filename = "Task1--" + DateTime.Now.ToString("MM-dd-hh-mm") + ".txt";
            logfile = new Log(filename, "Task 1 -- Thinking");
            logfile.logCondition(condition);
            try
            {
                gazeControl = new GazeControl(ip, port);
                
                //The file should be in the output box
                kinect = new KinectAudio("XMLs\\Thinking_Grammar.xml", ip, port);
            }
            catch (IOException)
            {
                Console.WriteLine("Kinect Not Connected");
            }
            taskThread = new Thread(new ThreadStart(startTask));
            end = false;
            taskThread.Start();
        }

        /// <summary>
        /// Task Loop 
        /// </summary>
        public void startTask()
        {
            if (!kinect.kinnectStatus)
                kinect.RestartKinect();
            //Add subscribers to the list
            subscribeCallBacks();
            kinect.StartKinect();
            Thread.Sleep(3000);
            led.off("ChestLeds");
            led.on("ChestLedsBlue");
            gazeControl.gazeControlConfiguration(condition);
            kinect.StartKinectSpeechHidden();
            gazeControl.start();
            while (!end)
            {
                Thread.Sleep(100);
            }
            gazeControl.end();
            this.endWindow();
        }

        private void respond(string y)
        {
            kinect.EndKinectSpeechHidden();
            string respondphrase = "";
            switch (y)
            {
                case "Question1":
                    respondphrase = "/home/nao/audio/thinking/thinking_1.wav";
                    completeCheck.update(1);
                    kinect.clearSubscribers();
                    kinect.Subscribe("Question1", x => repeatCallBack(x));
                    break;
                case "Question2":
                    respondphrase = "/home/nao/audio/thinking/thinking_2.wav";
                    completeCheck.update(2);
                    kinect.clearSubscribers();
                    kinect.Subscribe("Question2", x => repeatCallBack(x));
                    break;
                case "Question3":
                    respondphrase = "/home/nao/audio/thinking/thinking_3.wav";
                    completeCheck.update(3);
                    kinect.clearSubscribers();
                    kinect.Subscribe("Question3", x => repeatCallBack(x));
                    break;
                case "Question4":
                    respondphrase = "/home/nao/audio/thinking/thinking_4.wav";
                    completeCheck.update(4);
                    kinect.clearSubscribers();
                    kinect.Subscribe("Question4", x => repeatCallBack(x));
                    break;
                case "Question5":
                    respondphrase = "/home/nao/audio/thinking/thinking_5.wav";
                    completeCheck.update(5);
                    kinect.clearSubscribers();
                    kinect.Subscribe("Question5", x => repeatCallBack(x));
                    break;
            }

            Motion(respondphrase, y);

            if (! completeCheck.pass)
            {
                subscribeCallBacks();
                kinect.StartKinectSpeechHidden();
            }
            else
            {
                Console.WriteLine("Task 1 Ended");
                end = true;
            }
        }

        private void Motion(string path,string questionName)
        {
            //Initialize Varialbes
            this.repeat = false;
            this.interupt = false;
            this.happened = true;
            Random rnd = new Random();
            int wait = rnd.Next(70, 101); //Set a random time between 4 sec to 10 secs.
            //kinect.PrepKinectSound();
            gazeControl.setPath(path);

            //Subscribe to interrupt events.
            kinect.Subscribe("Interrupt", x => Interrupt());

            //Start gazing away.
            Thread motionThread = new Thread(() => gazeControl.startGaze(1));
            motionThread.Start();

            //kinect.StartKinectSound();
            kinect.StartKinectSpeechHidden();
            kinect.StartSpeechDetected();
            //Initialize the time to be recorded
            startTime = DateTime.Now;
            InteruptedTime = DateTime.Now;

            //Start the waiting and check for interupt every 100 milliseconds.
            for (var i = 0; i < wait; i++)
            {
                if (this.repeat == true)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            //measure the ending time
            endTime = DateTime.Now;
            kinect.EndKinectSpeechHidden();
            //kinect.EndKinectSound();
            kinect.Unsubscribe("Interrupt", x => Interrupt());
            //Log the variable.
            LogVariables(questionName);

            motionThread.Join();
            Thread returnMotion = new Thread(() => gazeControl.returnGaze(1,true));
            returnMotion.Start();
            returnMotion.Join();
        }

        protected void Interrupt()
        {
            Console.WriteLine("Interrupt Fired");
            //kinect.EndKinectSound();
            kinect.Unsubscribe("Interrupt", x => Interrupt());
            //Happen is putted to prevent events to be fired twice, defensive coding.
            if (happened)
            {
                InteruptedTime = DateTime.Now;
                Console.WriteLine("Time is " + DateTime.Now.ToString("ss.fff"));
                //True is to allow the code know an interrupt has happen.
                this.interupt = true;
                happened = false;
                Console.WriteLine("Interrupted");
            }
        }

        protected void LogVariables(string questionName)
        {
            //Logging all the information about the time.
            if (interupt == true)
            {
                logfile.write("Question " + questionName);
                logfile.logTime("startTime", startTime);
                logfile.logTime("Interupted", InteruptedTime);
            }
            else
            {
                logfile.write("Question " + questionName);
                logfile.logTime("startTime", startTime);
                logfile.logTime("EndTime", endTime);
            }
        }

        protected void repeatCallBack(string x)
        {
            Console.WriteLine(x + " has repeated");
            //InteruptedTime = DateTime.Now;
            this.repeat = true;
        }

        private void subscribeCallBacks()
        {
            kinect.clearSubscribers();
            kinect.Subscribe("Question1", x => respond(x));
            kinect.Subscribe("Question2", x => respond(x));
            kinect.Subscribe("Question3", x => respond(x));
            kinect.Subscribe("Question4", x => respond(x));
            kinect.Subscribe("Question5", x => respond(x));
        }

        #endregion
    }
}
