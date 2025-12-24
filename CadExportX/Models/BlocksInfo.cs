using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ModelSpace
{
    [Serializable]
    public class BlocksInfo : ContextBoundObject
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
                double bY = 6249.0;
                double bX = 8713.0;

                for (int y = 0; y < 17; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        if (Y < (bY - (bY * y)) && Y > (bY - (bY * (y + 1))))
                        {
                            if (X > (bX * x) && X < (bX * (x + 1)))
                            {
                                return (x + 1) + (y * 10);
                            }
                        }
                    }
                }
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
                return Parameters_.ToList().Find(x => x.Name == "PAGE-00-00-TAG")?.Value ?? "...";
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
                var parts = this.Name?.Split('-') ?? Array.Empty<string>();
                if (parts.Length > 0 && parts[0] == "SIG")
                {
                    string from = Parameters_.ToList().Find(x => x.Name.Contains("SIGNAL_FROM"))?.Value ?? "...";
                    string to = Parameters_.ToList().Find(x => x.Name.Contains("SIGNAL_TO"))?.Value ?? "...";
                    return $"{from}<=>{to}";
                }
                else if (parts.Length > 0 && parts[0] == "CAB")
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
            var parts = Name?.Split('-');
            string level = (parts != null && parts.Length > 1) ? parts[1] : "???";
            return Parameters_?.ToList()?.Find(x => x.Name == $"TAB-{level}-10-SCOPE")?.Value ?? "???";
        }

        public void SetParam(string pr, string val)
        {
            if (Parementers.ToList().Exists(x => x.Name == pr) && !string.IsNullOrEmpty(val))
                Parementers.ToList().Find(x => x.Name == pr).Value = val;
        }

        private ObservableCollection<BlockParam> Parameters_ = [];
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