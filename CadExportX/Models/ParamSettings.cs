using System;
using System.Runtime.CompilerServices;
using Cm = System.ComponentModel;

namespace ModelSpace
{
    [Serializable]
    public class ParamSettings : Cm.INotifyPropertyChanged
    {
        private string Name_;

        public string Name
        {
            get { return Name_; }
            set
            {
                Name_ = value;
                NotifyPropertyChanged();
            }
        }

        private Boolean Enable_;

        public Boolean Enable
        {
            get { return Enable_; }
            set
            {
                Enable_ = value;
                NotifyPropertyChanged();
            }
        }

        public event Cm.PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new Cm.PropertyChangedEventArgs(propertyName));
        }
    }
}