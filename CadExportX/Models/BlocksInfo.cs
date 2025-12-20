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
            try
            {
                if (string.IsNullOrEmpty(name))
                    return "...";
                
                if (name == "ID")
                    return Id.ToString();
                else if (name == "BLOCK_NAME")
                    return Name ?? "...";
                else if (name == "BL_SH")
                    return Sh.ToString();
                else if (name == "BL_PATH")
                    return PagePath ?? "...";
                else if (name == "KKS")
                    return Parameters_?.ToList()?.Find(x => x != null && x.Name != null && x.Name.Contains("TAG"))?.Value ?? "???";
                else if (name == "CORD_X")
                    return X.ToString("F2");
                else if (name == "CORD_Y")
                    return Y.ToString("F2");
                else if (name == "SCOPE")
                    return GetScope();
                else if (name == "FOLDER")
                    return Sub ?? "...";
                
                return Parameters_?.ToList()?.Find(x => x != null && x.Name == name)?.Value ?? "...";
            }
            catch (Exception)
            {
                return "...";
            }
        }

        public string GetValueAprox(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return "...";
                
                if (name.Contains("ID"))
                    return Id.ToString();
                else if (name.Contains("BLOCK_NAME"))
                    return Name ?? "...";
                else if (name.Contains("BL_SH"))
                    return Sh.ToString();
                else if (name.Contains("BL_PATH"))
                    return PagePath ?? "...";
                else if (name.Contains("KKS"))
                    return Parameters_?.ToList()?.Find(x => x != null && x.Name != null && x.Name.Contains("TAG"))?.Value ?? "???";
                else if (name.Contains("CORD_X"))
                    return X.ToString("F2");
                else if (name.Contains("CORD_Y"))
                    return Y.ToString("F2");
                else if (name.Contains("SCOPE"))
                    return GetScope();
                else if (name.Contains("FOLDER"))
                    return Sub ?? "...";
                else if (name.Contains("PATH"))
                {
                    if (string.IsNullOrEmpty(this.Name) || !this.Name.Contains('-'))
                        return "...";
                    
                    string[] parts = this.Name.Split('-');
                    if (parts.Length == 0)
                        return "...";
                    
                    string prefix = parts[0];
                    
                    if (prefix == "SIG")
                    {
                        string from = Parameters_?.ToList()?.Find(x => x != null && x.Name != null && x.Name.Contains("SIGNAL_FROM"))?.Value ?? "...";
                        string to = Parameters_?.ToList()?.Find(x => x != null && x.Name != null && x.Name.Contains("SIGNAL_TO"))?.Value ?? "...";
                        return $"{from}<=>{to}";
                    }
                    else if (prefix == "CAB")
                    {
                        string from = Parameters_?.ToList()?.Find(x => x != null && x.Name != null && x.Name.Contains("CABLE_FROM"))?.Value ?? "...";
                        string to = Parameters_?.ToList()?.Find(x => x != null && x.Name != null && x.Name.Contains("CABLE_TO"))?.Value ?? "...";
                        return $"{from}<=>{to}";
                    }
                    else
                    {
                        return "...";
                    }
                }
                
                return Parameters_?.ToList()?.Find(x => x != null && x.Name != null && x.Name.Contains(name))?.Value ?? "...";
            }
            catch (Exception)
            {
                return "...";
            }
        }

        public string GetScope()
        {
            try
            {
                if (string.IsNullOrEmpty(Name) || !Name.Contains('-'))
                    return "???";
                
                string[] parts = Name.Split('-');
                if (parts.Length < 2)
                    return "???";
                
                string level = parts[1];
                return Parameters_?.ToList()?.Find(x => x != null && x.Name == $"TAB-{level}-10-SCOPE")?.Value ?? "???";
            }
            catch (Exception)
            {
                return "???";
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
            return Name + " \\ " + GetValueAprox("TAG");
        }
    }
}