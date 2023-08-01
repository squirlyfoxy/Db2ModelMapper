using System;

namespace Db2ModelMapper.Core.Utils
{
    public class LoggerEventArgs : EventArgs
    {
        public LoggerEventArgs(string msg, Exception ex = null) 
        { 
            Exception = ex;
            Message = msg;
        }

        public string Message { get; set; }

        public Exception Exception { get; set; }
    }
}
