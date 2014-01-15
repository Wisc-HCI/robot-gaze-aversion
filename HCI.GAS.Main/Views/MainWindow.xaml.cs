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
using System.Windows.Shapes;

namespace HCI.GAS.Main.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            model = new ViewModels.ViewModel();
            this.DataContext = model;

        }
        private ViewModels.ViewModel model;
        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (model != null)
            {
                model.close_clear();
                model = null;
            }
            Application.Current.Shutdown();
        }

    }
}
