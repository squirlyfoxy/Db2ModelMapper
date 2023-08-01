using System;

namespace Db2ModelMapper.Core.Utils
{
    public class Logger
    {
        public static event EventHandler<LoggerEventArgs> InfoEvent;
        public static event EventHandler<LoggerEventArgs> ErrorEvent;
        public static event EventHandler<LoggerEventArgs> WarningEvent;
        public static event EventHandler<LoggerEventArgs> DebugEvent;

        public static void Error(string msg, Exception ex = null)
        {
            if (ErrorEvent != null)
            {
                ErrorEvent(null, new LoggerEventArgs(msg, ex));
            }
        }

        public static void Info(string msg, Exception ex = null)
        {
            if (InfoEvent != null)
            {
                InfoEvent(null, new LoggerEventArgs(msg, ex));
            }
        }

        public static void Warn(string msg, Exception ex = null)
        {
            if (WarningEvent != null)
            {
                WarningEvent(null, new LoggerEventArgs(msg, ex));
            }
        }

        public static void Debug(string msg, Exception ex = null)
        {
            if (DebugEvent != null)
            {
                DebugEvent(null, new LoggerEventArgs(msg, ex));
            }
        }
    }
}
