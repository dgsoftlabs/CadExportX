using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ModelSpace
{
    [Serializable]
    public class BlocksInfo
    {
        public string Tag
        {
            get
            {
                return GetValue("TAG");
            }
        }

        public string Name { get; set; }
        public string Sub { get; set; }
        public long Id { get; set; }

        public int Sh
        {
            get
            {
                return 0;
            }
        }

        public double X { get; set; }
        public double Y { get; set; }
        public string PagePath { get; set; }

        public string GetValue(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return "...";

                return Parameters_?.ToList()?.Find(x => x != null && x.Name == name)?.Value ?? "...";
            }
            catch (Exception)
            {
                return "...";
            }
        }

        public void SetParam(string pr, string val)
        {
            if (Parementers.ToList().Exists(x => x.Name == pr) && !string.IsNullOrEmpty(val))
                Parementers.ToList().Find(x => x.Name == pr).Value = val;
        }

        private ObservableCollection<BlockParam> Parameters_ = new ObservableCollection<BlockParam>();

        public ObservableCollection<BlockParam> Parementers
        {
            get { return Parameters_; }
            set { Parameters_ = value; }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}