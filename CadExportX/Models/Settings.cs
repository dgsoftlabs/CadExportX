using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Cm = System.ComponentModel;

namespace ModelSpace
{
    [Serializable]
    public class Settings : Cm.INotifyPropertyChanged
    {
        public Settings()
        {
        }

        public Settings(BlocksInfo el)
        {
            Name = el.Name;
            Enable = true;

            Params_ = new ObservableCollection<ParamSettings>();
            foreach (var p in el.Parementers.OrderBy(x => x.Name))
                Params.Add(new ParamSettings() { Name = p.Name, Enable = true });
        }

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

                if (Params?.Count > 0)
                {
                    foreach (var p in Params)
                        p.Enable = value;
                }

                NotifyPropertyChanged();
            }
        }

        private ObservableCollection<ParamSettings> Params_;

        public ObservableCollection<ParamSettings> Params
        {
            get { return Params_; }
            set { Params_ = value; }
        }

        public event Cm.PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new Cm.PropertyChangedEventArgs(propertyName));
        }
    }
}