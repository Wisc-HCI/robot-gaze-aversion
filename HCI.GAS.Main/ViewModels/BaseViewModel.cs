using HCI.GAS.Main.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCI.GAS.Main.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected static Common coms = new Common();

        public BaseViewModel():base()
        {

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(
                    this, new PropertyChangedEventArgs(propName));
        }

        #endregion
    }
}
