using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelSpace
{
    [Serializable]
    public class PageInfo
    {
        public string Path { get; set; }
        public string Sub { get; set; }
        public Boolean IsChanged { get; set; }

        public string GetPage()
        {
            if (Blocks.Count > 0)
            {
                return Blocks.First().Sh.ToString();
            }
            else
            {
                return "???";
            }
        }

        public string GetFileName()
        {
            if (!string.IsNullOrEmpty(Path))
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            else
                return "???";
        }

        private ChangesKind Change_;

        public ChangesKind Change
        {
            get { return Change_; }
            set { Change_ = value; }
        }

        private List<BlocksInfo> Blocks_ = new List<BlocksInfo>();

        public List<BlocksInfo> Blocks
        {
            get { return Blocks_; }
            set { Blocks_ = value; }
        }

        public override string ToString()
        {
            return GetPage();
        }
    }
}