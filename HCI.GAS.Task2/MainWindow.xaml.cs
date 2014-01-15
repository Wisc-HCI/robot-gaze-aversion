using System;
using System.Collections.Generic;
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
using Aldebaran.Proxies;
using HCI.Nao.Gaze;

namespace HCI.GAS.Task2
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
        private HCI.GAS.Kinect.KinectAudio kinect;
        private GazeControl gazeControl;
        private TextToSpeechProxy tts;
        private LedsProxy led;
        private Check.Check completeCheck;
        private bool end;
        private Thread taskThread;
        private bool ready;

        #endregion

        public MainWindow(string ip, int port, int condition)
        {
            this.ip = ip;
            this.port = port;
            this.condition = condition;
            InitializeComponent();
            completeCheck = new Check.Check();
            ready = true;

            this.Background = new SolidColorBrush(Colors.LightGreen);

            gazeControl = new GazeControl(ip, port);
            kinect = new HCI.GAS.Kinect.KinectAudio("XMLs\\Thoughtfulness_Grammar.xml",ip,port);
            led = new LedsProxy(ip, port);
            led.off("ChestLeds");
            led.on("ChestLedsGreen");
            
            //TODO: check if auto has been depreciated.
            kinect.auto = false;

            end = false;
            tts = new TextToSpeechProxy(ip, port);
        }

        ~MainWindow()
        {
            if (kinect != null)
                kinect.EndKinect();
            kinect = null;
        }

        #region GUI Related

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
            if (gazeControl != null)
                gazeControl.end();
            if (kinect != null)
                kinect.EndKinect();
            LedsProxy led = new LedsProxy(ip, port);
            led.off("ChestLeds");
            led.on("ChestLedsRed");
            kinect = null;
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ready)
            {
                ready = false;
                if (taskThread == null)
                {
                    this.Background = new SolidColorBrush(Colors.LightBlue);
                    led.off("ChestLeds");
                    taskThread = new Thread(startTask);
                    taskThread.Start();
                }
                else if (!completeCheck.pass)
                {
                    kinect.StartKinectSpeech();
                }
                else
                {
                    end = true;
                }
            }
            else
            {
                Console.WriteLine("Unneeded Mouse Clicks");
            }
        }

        #endregion

        #region task related

        private void startTask()
        {
            kinect.Subscribe("Question1", x => respond(x));
            kinect.Subscribe("Question2", x => respond(x));
            kinect.Subscribe("Question3", x => respond(x));
            kinect.Subscribe("Question4", x => respond(x));
            kinect.Subscribe("Question5", x => respond(x));
            kinect.StartKinect();
            Thread.Sleep(3000);
            gazeControl.gazeControlConfiguration(condition);
            gazeControl.start();
            kinect.StartKinectSpeech();
            while (!end)
            {
                Thread.Sleep(200);
            }
            endWindow();
        }

        private void respond(string y)
        {
            kinect.EndKinectSpeech();
            string respondphrase = "";
            switch (y)
            {
                case "Question1":
                    respondphrase = "/home/nao/audio/thoughtfulness/thoughtfulness_1.wav";
                    //kinect.Unsubscribe("Question1", x => respond(x));
                    completeCheck.update(1);
                    break;
                case "Question2":
                    respondphrase = "/home/nao/audio/thoughtfulness/thoughtfulness_2.wav";
                    //kinect.Unsubscribe("Question2", x => respond(x));
                    completeCheck.update(2);
                    break;
                case "Question3":
                    respondphrase = "/home/nao/audio/thoughtfulness/thoughtfulness_3.wav";
                    //kinect.Unsubscribe("Question3", x => respond(x));
                    completeCheck.update(3);
                    break;
                case "Question4":
                    respondphrase = "/home/nao/audio/thoughtfulness/thoughtfulness_4.wav";
                    //kinect.Unsubscribe("Question4", x => respond(x));
                    completeCheck.update(4);
                    break;
                case "Question5":
                    respondphrase = "/home/nao/audio/thoughtfulness/thoughtfulness_5.wav";
                    //kinect.Unsubscribe("Question5", x => respond(x));
                    completeCheck.update(5);
                    break;
            }
            respondAction(respondphrase);
            ready = true;
        }

        private void respondAction(string respond)
        {
            gazeControl.setPath(respond);
            gazeControl.startGaze(2);
        }

        #endregion
    }
}
