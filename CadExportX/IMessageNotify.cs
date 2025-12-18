using System;

namespace ModelSpace
{
    public interface IMessageNotify
    {
        event EventHandler Message;
    }
}