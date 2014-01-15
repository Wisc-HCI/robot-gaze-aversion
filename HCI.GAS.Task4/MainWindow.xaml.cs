using Aldebaran.Proxies;
using HCI.Kinect;
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
using HCI.GAS.Logs;
using HCI.Nao.Gaze;

namespace HCI.GAS.Task4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Variable

        private string ip;
        private int port;
        private int condition;
        private bool started; //To signal the task has started
        private bool ready; //task ready for input
        private Log logfile; // log variables   

        private HCI.GAS.Kinect.KinectAudio kinect; //kinect audio
        private LedsProxy led; //control led of Nao's chest button
        private GazeControl gazeControl; //gaze contorl 
        private Check.Check completeCheck; //to check if the task has been completed
        private bool end; //Ending flag
        private bool interupted; //flag raise when the pause is been interupted.

        DateTime startTime;
        DateTime interuptTime;
        DateTime endTime;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            completeCheck = new Check.Check();
            ready = true;
            this.Background = new SolidColorBrush(Colors.LightGreen);
        }
        public MainWindow(string ip,int port,int condition):this()
        {
            this.ip = ip;
            this.port = port;
            led = new LedsProxy(ip, port);
            led.off("ChestLeds");
            led.on("ChestLedsGreen");
            this.condition = condition;
            this.started = false;
            this.interupted = false;
        }

        #region GUI Related

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ready)
            {
                ready = false;
                if (!started)
                {
                    led.off("ChestLeds");
                    started = true;
                    task();
                }
                else if (!completeCheck.pass)
                {
                    kinect.StartKinectSpeech();
                }
                else
                {
                    end = true;
                    endWindow();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Unneeded Mouse Clicks");
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
            if(logfile != null)
                logfile.End();
            if (led != null)
            {
                led.off("ChestLeds");
                led.on("ChestLedsRed");
            }
            if(kinect != null)
                kinect.EndKinect();
        }

        #endregion

        #region Task Related

        private void Interrupt()
        {
            this.interupted = true;
            interuptTime = DateTime.Now;
        }

        private Thread taskThread = null;
        public void task()
        {
            string filename = "Task4--" + DateTime.Now.ToString("MM-dd-hh-mm") + ".txt";
            logfile = new Log(filename, "Task 4 -- Turn Taking");
            logfile.logCondition(condition);
            try
            {
                gazeControl = new GazeControl(ip, port);
                kinect = new HCI.GAS.Kinect.KinectAudio("XMLs\\Turntaking_Grammar.xml", ip, port);
                kinect.auto = false;
            }
            catch (IOException)
            {
                Console.WriteLine("Kinect Not Connected");
            }
            end = false;
            taskThread = new Thread(new ThreadStart(startTask));
            this.Background = new SolidColorBrush(Colors.LightBlue);
            taskThread.Start();
        }

        public void startTask()
        {
            if (!kinect.kinnectStatus)
                kinect.RestartKinect();
            kinect.Subscribe("Question1", x => speak(x));
            kinect.Subscribe("Question2", x => speak(x));
            kinect.Subscribe("Question3", x => speak(x));
            kinect.Subscribe("Question4", x => speak(x));
            kinect.Subscribe("Question5", x => speak(x));
            kinect.StartKinect();
            Thread.Sleep(3000);
            gazeControl.gazeControlConfiguration(condition);
            gazeControl.start();
            kinect.StartKinectSpeech();
            while (!end)
            {
                Thread.Sleep(1000);
            }
            endWindow();
        }

        private void speak(string key)
        {
            kinect.EndKinectSpeech();
            string phrase1 = "";
            string phrase2 = "";
            switch (key)
            {
                case "Question1":
                    phrase1 = "/home/nao/audio/turntaking/turntaking_1a.wav";
                    phrase2 = "/home/nao/audio/turntaking/turntaking_1b.wav";
                    completeCheck.update(1);
                    break;

                case "Question2":
                    phrase1 = "/home/nao/audio/turntaking/turntaking_2a.wav";
                    phrase2 = "/home/nao/audio/turntaking/turntaking_2b.wav";
                    completeCheck.update(2);
                    break;

                case "Question3":
                    phrase1 = "/home/nao/audio/turntaking/turntaking_3a.wav";
                    phrase2 = "/home/nao/audio/turntaking/turntaking_3b.wav";
                    completeCheck.update(3);
                    break;

                case "Question4":
                    phrase1 = "/home/nao/audio/turntaking/turntaking_4a.wav";
                    phrase2 = "/home/nao/audio/turntaking/turntaking_4b.wav";
                    completeCheck.update(4);
                    break;

                case "Question5":
                    phrase1 = "/home/nao/audio/turntaking/turntaking_5a.wav";
                    phrase2 = "/home/nao/audio/turntaking/turntaking_5b.wav";
                    completeCheck.update(5);
                    break;
            }
            kinect.Subscribe("Interrupt", x => Interrupt());
            Motion(phrase1, phrase2, key);
            led.off("ChestLeds");
            this.interupted = false;
            kinect.Unsubscribe("Interrupt", x => Interrupt());
            ready = true;
            gazeControl.resumeIntimacy();
        }
        private void Motion(string phrase1, string phrase2, string key)
        {
            kinect.PrepKinectSound();
            gazeControl.setPath(phrase1, phrase2);
            Thread motionThread = new Thread(() => gazeControl.startGaze(4));
            motionThread.Start();
            //REMINDER: the thread ends when the speech ends.
            motionThread.Join();
            startTime = DateTime.Now;
            //led.on("ChestLedsBlue");
            kinect.StartKinectSound();
            Random rnd = new Random();
            int wait = rnd.Next(20, 41);
            for (var i = 0; i < wait; i++)
            {
                if (this.interupted)
                {
                    Console.WriteLine("Interrupt Fired");
                    kinect.EndKinectSound();
                    logVariables(key);
                    gazeControl.returnGaze(4,false);
                    return;
                }
                Thread.Sleep(100);
            }
            endTime = DateTime.Now;
            logVariables(key);
            kinect.EndKinectSound();
            gazeControl.returnGaze(4,true);
            kinect.StartSpeechDetected();
            while (true)
            {
                if (this.interupted)
                {
                    Console.WriteLine("Interrupt Fired");
                    kinect.EndSpeechDetected();
                    return;
                }
                Thread.Sleep(100);
            }
        }

        private void logVariables(string question)
        {
            //Logging all the information about the time.
            if (interupted == true)
            {
                logfile.write("Question " + question);
                logfile.logTime("startTime", startTime);
                logfile.logTime("Interupted", interuptTime);
            }
            else
            {
                logfile.write("Question " + question);
                logfile.logTime("startTime", startTime);
                logfile.logTime("EndTime", endTime);
            }
        }

        #endregion
    }
}
