using System;
using System.Collections.Generic;

using System.Text;
using System.Diagnostics;

namespace Game.COSXML.Log
{
    public sealed class LogImpl : ILog
    {

        public void PrintLog(string message)
        {
            Trace.WriteLine(message);
        }
    }
}
