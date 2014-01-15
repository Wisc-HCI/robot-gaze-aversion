using HCI.GAS.Main.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;
using Aldebaran.Proxies;
using Basic;
using HCI.GAS.Main.Commands;
using HCI.GAS.Main.Views;
using System.Windows;
using System.Collections;
using HCI.GAS.Main.Event;
using System.Runtime.InteropServices;
using Microsoft.Kinect;

namespace HCI.GAS.Main.ViewModels
{
    class ViewModel :BaseViewModel
    {
        public ViewModel():base()
        {
            initializeCommands();
            subscribeEvent();
            nao_Condition = 2;
        }

        #region Setter & Getter
        public string nao_IP
        {
            get
            {
                
                string ip = coms.naoStatus.ip;
                if (ip != null)
                    return ip;
                else
                    return null;
            }
            set
            {
                coms.naoStatus.ip = value;
            }
        }

        public int nao_Port
        {
            get
            {
                return coms.naoStatus.port;
            }
            set
            {
                coms.naoStatus.port = value;
            }
        }

        public int nao_Condition
        {
            get
            {
                return coms.naoStatus.condition;
            }
            set
            {
                coms.naoStatus.condition = value;
                this.OnPropertyChanged("nao_Condition");
                var msg = new Messenger("condition");
                coms.EventMessage.Publish<Messenger>(msg);
            }
        }

        public bool nao_status
        {
            get
            {
                return coms.naoStatus.status;      
            }
            set
            {
                coms.naoStatus.status = value;
                this.OnPropertyChanged("nao_status_color");
            }
        }

        public bool kinect_status
        {
            get { return coms.kinectStatus; }
            set { coms.kinectStatus = value; this.OnPropertyChanged("kinect_status_color"); }
        }

        public string comboBox
        {
            set
            {
                switch(value)
                {
                    case "No Motion":
                        nao_Condition = 1;
                        break;
                    case "Correct Motion":
                        nao_Condition = 2;
                        break;
                    case "Wrong Motion":
                        nao_Condition = 3;
                        break;
                }

                switch (nao_Condition)
                {
                    case 1:
                        value = "No Motion";
                        break;
                    case 2:
                        value = "Correct Motion";
                        break;
                    case 3:
                        value = "Wrong Motion";
                        break;
                }
                var msg = new Messenger("condition");
                coms.EventMessage.Publish<Messenger>(msg);

            }
        }

        #endregion

        #region commands

        private void initializeCommands()
        {
            _naoPosition = new Command
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => position_nao()
            };

            _connectNao = new Command
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => connect_nao()
            };

            _openOptionWindow = new Command
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => option_window()
            };

            _closeOptionWindow = new Command
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => close_window("option_w")
            };

            _openTask1 = new Command
            {
                CanExecuteDelegate = x => (nao_status && kinect_status),
                ExecuteDelegate = x => { ledOff(); openTaskWindow(1); }
            };

            _openTask2 = new Command
            {
                CanExecuteDelegate = x => (nao_status && kinect_status),
                ExecuteDelegate = x => { ledOff(); openTaskWindow(2); }
            };

            _openTask3 = new Command
            {
                CanExecuteDelegate = x => (nao_status && kinect_status),
                ExecuteDelegate = x => { ledOff(); openTaskWindow(3); }
            };

            _openTask4 = new Command
            {
                CanExecuteDelegate = x => (nao_status && kinect_status),
                ExecuteDelegate = x => { ledOff(); openTaskWindow(4); }
            };

            _onloaded = new Command
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => loadVar()
            };
        }

        private ICommand _onloaded;
        public ICommand onloaded
        {
            get
            {
                return _onloaded;
            }
        }

        private ICommand _openTask1;
        public ICommand openTask1
        {
            get
            {
                return _openTask1;
            }
        }

        private ICommand _openTask2;
        public ICommand openTask2
        {
            get
            {
                return _openTask2;
            }
        }

        private ICommand _openTask3;
        public ICommand openTask3
        {
            get
            {
                return _openTask3;
            }
        }

        private ICommand _openTask4;
        public ICommand openTask4
        {
            get
            {
                return _openTask4;
            }
        }


        private ICommand _connectNao;
        public ICommand connectNao
        {
            get
            {
                return _connectNao;
            }
        }
        private ICommand _naoPosition;
        public ICommand naoPosition
        {
            get
            {
                return _naoPosition;
            }
        }

        private ICommand _openOptionWindow;
        public ICommand openOptionWindow
        {
            get
            {
                return _openOptionWindow;
            }
        }

        private ICommand _closeOptionWindow;
        public ICommand closeOptionWindow
        {
            get
            {
                return _closeOptionWindow;
            }
        }

        #endregion  

        #region command methods

        private void loadVar()
        {
            nao_Condition = 2;
            nao_IP = "192.168.1.112";
            nao_Port = 9559;
        }

        private void option_window()
        {
            if (!coms.OpenedWindow.Contains("option_w"))
            {
                var window = new option();
                coms.OpenedWindow.Add("option_w", window);
                window.Show();
            }
            else
            {
                var window = (option)coms.OpenedWindow["option_w"];
                window.Show();
                
            }
            
        }

        private void close_window(string win)
        {
            if (coms.OpenedWindow.ContainsKey(win))
            {
                Window window = (Window)coms.OpenedWindow[win];
                coms.OpenedWindow.Remove(win);
                window.Close();
            }
        }

        public void close_clear()
        {
            coms.OpenedWindow.Clear();
            coms = null;
        }


        private void openTaskWindow(int task)
        {
            try
            {
                switch (task)
                {
                    case 1:
                        var task1 = new HCI.GAS.Task1.MainWindow(nao_IP, nao_Port, nao_Condition);
                        task1.Show();
                        break;
                    case 2:
                        var task2 = new HCI.GAS.Task2.MainWindow(nao_IP, nao_Port, nao_Condition);
                        task2.Show();
                        break;
                    case 3:
                        var task3 = new HCI.GAS.Task3.MainWindow(nao_IP, nao_Port, nao_Condition);
                        task3.Show();
                        break;
                    case 4:
                        var task4 = new HCI.GAS.Task4.MainWindow(nao_IP, nao_Port, nao_Condition);
                        task4.Show();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception has happen in one of the Windows");
                Console.WriteLine(e.Message);
            }
        }


        #endregion

        #region Logics related to MainWindow
        public void position_nao()
        {
            if (coms.naoStatus.ip == null || coms.naoStatus.port == 0)
            {
                nao_status = false;
            }
            else
            {
                try
                {


                    tools tool = new tools(coms.naoStatus.ip, coms.naoStatus.port);
                    behavior manager = new behavior(coms.naoStatus.ip, coms.naoStatus.port);

                    if (manager.start("initialPosition_SitStraight"))
                    {

                        nao_status = true;
                    }
                    else
                    {
                        nao_status = false;
                    }
                }
                catch (Exception)
                {
                    nao_status = false;
                }
            }
            kinect_status = false;
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    kinect_status = true;
                    break;
                }
            }
        }

        public void connect_nao()
        {
            if (coms.naoStatus.ip == null || coms.naoStatus.port == 0)
            {
                nao_status = false;
            }
            else
            {
                try
                {
                    tools tool = new tools(coms.naoStatus.ip, coms.naoStatus.port);
                    if (tool.tryConnection())
                    {

                        nao_status = true;
                    }
                    else
                    {
                        nao_status = false;
                    }
                }
                catch (Exception)
                {
                    nao_status = false;
                }
            }
            kinect_status = false;
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    kinect_status = true;
                    break;
                }
            }
        }

        public void ledOff()
        {
            LedsProxy led = new LedsProxy(coms.naoStatus.ip, coms.naoStatus.port);
            led.off("ChestLeds");
        }

        public SolidColorBrush nao_status_color
        {
            get
            {
                if (coms.naoStatus.status)
                {
                    return new SolidColorBrush(Colors.LightGreen);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
        }

        public SolidColorBrush kinect_status_color
        {
            get
            {
                if (coms.kinectStatus)
                {
                    return new SolidColorBrush(Colors.LightGreen);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
        }
        #endregion
        #region Events

        public void subscribeEvent()
        {
            coms.EventMessage.Subscribe<Messenger>( e =>checks(e) );
        }

        public void checks(Messenger e)
        {
            switch(e.name)
            {
                case "condition":
                    this.OnPropertyChanged("nao_Condition");
                    this.OnPropertyChanged("comboBox");
                    break;
            }
        }

        #endregion

    }
}
