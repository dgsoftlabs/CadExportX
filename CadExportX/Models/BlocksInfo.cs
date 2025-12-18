using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ModelSpace
{
    [Serializable]
    public class BlocksInfo
    {
        public BlocksInfo()
        {
        }

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
            if (name == "ID")
                return Id.ToString();
            else if (name == "BLOCK_NAME")
                return Name;
            else if (name == "BL_SH")
                return Sh.ToString();
            else if (name == "BL_PATH")
                return PagePath;
            else if (name == "KKS")
                return Parameters_?.ToList()?.Find(x => x.Name.Contains("TAG"))?.Value ?? "???";
            else if (name == "CORD_X")
                return X.ToString("F2");
            else if (name == "CORD_Y")
                return Y.ToString("F2");
            else if (name == "SCOPE")
                return GetScope();
            else if (name == "FOLDER")
                return Sub;

            return Parameters_.ToList().Find(x => x.Name == name)?.Value ?? "...";
        }

        public string GetValueAprox(string name)
        {
            if (name.Contains("ID"))
                return Id.ToString();
            else if (name.Contains("BLOCK_NAME"))
                return Name;
            else if (name.Contains("BL_SH"))
                return Sh.ToString();
            else if (name.Contains("BL_PATH"))
                return PagePath;
            else if (name.Contains("KKS"))
                return Parameters_?.ToList()?.Find(x => x.Name.Contains("TAG"))?.Value ?? "???";
            else if (name.Contains("CORD_X"))
                return X.ToString("F2");
            else if (name.Contains("CORD_Y"))
                return Y.ToString("F2");
            else if (name.Contains("SCOPE"))
                return GetScope();
            else if (name.Contains("FOLDER"))
                return Sub;
            else if (name.Contains("PATH"))
            {
                if (this.Name.Split('-')[0] == "SIG")
                {
                    string from = Parameters_.ToList().Find(x => x.Name.Contains("SIGNAL_FROM"))?.Value ?? "...";
                    string to = Parameters_.ToList().Find(x => x.Name.Contains("SIGNAL_TO"))?.Value ?? "...";
                    return $"{from}<=>{to}";
                }
                else if (this.Name.Split('-')[0] == "CAB")
                {
                    string from = Parameters_.ToList().Find(x => x.Name.Contains("CABLE_FROM"))?.Value ?? "...";
                    string to = Parameters_.ToList().Find(x => x.Name.Contains("CABLE_TO"))?.Value ?? "...";
                    return $"{from}<=>{to}";
                }
                else
                {
                    return "...";
                }
            }

            return Parameters_.ToList().Find(x => x.Name.Contains(name))?.Value ?? "...";
        }

        public string GetScope()
        {
            string level = Name?.Split('-')?[1] ?? "???";
            return Parameters_?.ToList()?.Find(x => x.Name == $"TAB-{level}-10-SCOPE")?.Value ?? "???";
        }

        public List<BlockParam> GetApproxParamList(string name)
        {
            return Parameters_.Where(x => x.Name.Contains(name))?.ToList();
        }

        public Guid GetRevId()
        {
            if (Parementers.ToList().Exists(x => x.Name == "PAGE-00-00-REV_ID"))
            {
                Guid g;
                if (Guid.TryParse(GetValue("PAGE-00-00-REV_ID"), out g))
                    return g;
                else
                    return Guid.Empty;
            }
            else return Guid.Empty;
        }

        public void SetParam(string pr, string val)
        {
            if (Parementers.ToList().Exists(x => x.Name == pr) && !string.IsNullOrEmpty(val))
                Parementers.ToList().Find(x => x.Name == pr).Value = val;
        }

        public BlockParam GetParamObj(string s)
        {
            if (s == "BL_SH")
            {
                return new BlockParam() { Name = "BL_SH", Value = Sh.ToString() };
            }
            else if (s == "SCOPE")
            {
                string level = Name?.Split('-')?[1] ?? "???";
                return Parameters_?.ToList()?.Find(x => x.Name == $"TAB-{level}-10-SCOPE") ?? new BlockParam() { Change = ChangesKind.NotChanged, Name = "NA", Value = "NA" };
            }
            else if (Parementers.ToList().Exists(x => x.Name == s))
            {
                return Parementers.ToList().Find(x => x.Name == s);
            }
            else return new BlockParam() { Name = "NA", Value = "NA" };
        }

        public void ResetRevParam()
        {
            foreach (var x in Parameters_)
                x.Change = ChangesKind.NotChanged;
        }

        private ChangesKind Change_;

        public ChangesKind Change
        {
            get { return Change_; }
            set { Change_ = value; }
        }

        private ObservableCollection<BlockParam> Parameters_ = new ObservableCollection<BlockParam>();

        public ObservableCollection<BlockParam> Parementers
        {
            get { return Parameters_; }
            set { Parameters_ = value; }
        }

        public override string ToString()
        {
            return Name + " \\ " + GetValueAprox("TAG");
        }
    }
}