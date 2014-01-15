using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Threading;

namespace HCI.GAS.Task3
{

    public partial class MainWindow : Window
    {

        #region Variables

        private string ip;
        private int port;
        private int condition;
        private int count;
        private TextToSpeechProxy tts;
        private LedsProxy led;
        private GazeControl gazeControl;
        private bool started;
        private bool ready;

        #endregion

        public MainWindow(string ip, int port, int condition)
        {
            count = 1;
            this.ip = ip;
            this.port = port;
            this.condition = condition;
            ready = true;

            this.Background = new SolidColorBrush(Colors.LightGreen);

            InitializeComponent();
            this.tts = new TextToSpeechProxy(ip, port);

            gazeControl = new GazeControl(ip, port);

            this.led = new LedsProxy(ip, port);
            led.off("ChestLeds");
            led.on("ChestLedsGreen");
            started = false;
        }

        #region task related

        private void speak()
        {
            string question = "";
            switch(count)
            {
                case 1:
                    question = "/home/nao/audio/disclosure/disclosure_1.wav";
                    gazeControl.setPath(question);
                    gazeControl.startGaze(2);
                    break;
                case 2:
                    question = "/home/nao/audio/disclosure/disclosure_2.wav";
                    gazeControl.setPath(question);
                    gazeControl.startGaze(3);
                    break;
                case 3:
                    question = "/home/nao/audio/disclosure/disclosure_3.wav";
                    gazeControl.setPath(question);
                    gazeControl.startGaze(2);
                    break;
                case 4:
                    question = "/home/nao/audio/disclosure/disclosure_4.wav";
                    gazeControl.setPath(question);
                    gazeControl.startGaze(2);
                    break;
                case 5:
                    question = "/home/nao/audio/disclosure/disclosure_5.wav";
                    gazeControl.setPath(question);
                    gazeControl.startGaze(2);
                    break;
            }
        }

        #endregion

        #region Control Functions

        private void start()
        {
            led.off("ChestLeds");
            gazeControl.gazeControlConfiguration(condition);
            gazeControl.start();
            started = true;
            //They share the same codes.
            continueTask();
        }

        private void continueTask()
        {
            led.off("ChestLeds");
            speak();
            //Resume Intimacy due to the possibility of listen mood not registering,
            //just to be safe
            gazeControl.resumeIntimacy();
            count++;
            ready = true;
            return;
        }

        #endregion

        #region GUi Related

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            endWindow();
        }

        public Thread taskThread;
        private void window_click(object sender, MouseEventArgs e)
        {
            if (ready)
            {
                ready = false;
                if (!started)
                {
                    //Manual Break to slow the code down.
                    Thread.Sleep(500);
                    taskThread = new Thread(start);
                    this.Background = new SolidColorBrush(Colors.LightBlue);
                    taskThread.Start();
                }
                else if (count == 6)
                {
                    this.Close();
                }
                else
                {
                    //Manual Break to be not so fast
                    Thread.Sleep(500);
                    taskThread = new Thread(continueTask);
                    taskThread.Start();
                }
            }
            else
            {
                Console.WriteLine("unwanted clicks");
            }
        }

        public void endWindow()
        {
            if(taskThread != null && taskThread.IsAlive)
                taskThread.Join();
            Console.WriteLine("End of Task 3 - Disclosure");
            if(gazeControl != null)
                gazeControl.end();
            if (led != null)
            {
                led.off("ChestLeds");
                led.on("ChestLedsRed");
            }
        }
        #endregion
    }
}
