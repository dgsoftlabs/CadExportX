using System;

namespace ModelSpace
{
    [Serializable]
    public class BlockParam
    {
        public BlockParam()
        {
        }

        public string Name { get; set; }
        public string Value { get; set; }

        private ChangesKind Change_;

        public ChangesKind Change
        {
            get { return Change_; }
            set { Change_ = value; }
        }
    }
}