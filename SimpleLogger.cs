using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace blockshift_ib
{
    public class SimpleLogger
    {
        private string DatetimeFormat;
        public string Filename;
        public bool display;
        private static readonly object _syncObject = new object();
        /// <summary>
        /// Initialize a new instance of SimpleLogger class.
        /// Log file will be created automatically if not yet exists, else it can be either a fresh new file or append to the existing file.
        /// Default is create a fresh new log file.
        /// </summary>
        /// <param name="append">True to append to existing log file, False to overwrite and create new log file</param>
        public SimpleLogger(bool append = true, bool display = true)
        {
            this.display = display;
            DatetimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
            Filename = Assembly.GetExecutingAssembly().GetName().Name + DateTime.Now.ToString("yyyyMMdd") + ".log";

            // Log file header line
            string logHeader;
            if (!File.Exists(Filename))
            {
                logHeader = Filename + " is created.";
                WriteLine(DateTime.Now.ToString(DatetimeFormat) + " " + logHeader, false);
            }
            else
            {
                if (append == false)
                {
                    logHeader = Filename + " is created.";
                    WriteLine(DateTime.Now.ToString(DatetimeFormat) + " " + logHeader, false);
                }
                else
                {
                    logHeader = "Process Started";
                    WriteLine(DateTime.Now.ToString(DatetimeFormat) + " " + logHeader, true);
                }
            }
        }
         /// <summary>
        /// Supported log level
        /// </summary>
        [Flags]
        private enum LogLevel
        {
            SIGNAL,
            DB,
            TRACE,
            INFO,
            BROKER,
            DEBUG,
            WARNING,
            ERROR,
            FATAL
        }

        /// <summary>
        /// Log a signal message
        /// </summary>
        /// <param name="text">Message</param>
        public void Broker(string text)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            WriteFormattedLog(LogLevel.BROKER, text);
            Console.ResetColor();
        }
        /// <summary>
        /// Log a signal message
        /// </summary>
        /// <param name="text">Message</param>
        public void Signal(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            WriteFormattedLog(LogLevel.SIGNAL, text);
            Console.ResetColor();
        }
        /// <summary>
        /// Log a signal message
        /// </summary>
        /// <param name="text">Message</param>
        public void Db(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            WriteFormattedLog(LogLevel.DB, text);
            Console.ResetColor();
        }
        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="text">Message</param>
        public void Debug(string text)
        {
            WriteFormattedLog(LogLevel.DEBUG, text);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="text">Message</param>
        public void Error(string text)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            WriteFormattedLog(LogLevel.ERROR, text);
            Console.ResetColor();
        }

        /// <summary>
        /// Log a fatal error message
        /// </summary>
        /// <param name="text">Message</param>
        public void Fatal(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteFormattedLog(LogLevel.FATAL, text);
            Console.ResetColor();
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        /// <param name="text">Message</param>
        public void Info(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteFormattedLog(LogLevel.INFO, text);
            Console.ResetColor();
        }

        /// <summary>
        /// Log a trace message
        /// </summary>
        /// <param name="text">Message</param>
        public void Trace(string text)
        {
            WriteFormattedLog(LogLevel.TRACE, text);
        }

        /// <summary>
        /// Log a waning message
        /// </summary>
        /// <param name="text">Message</param>
        public void Warning(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            WriteFormattedLog(LogLevel.WARNING, text);
            Console.ResetColor();
        }

        /// <summary>
        /// Format a log message based on log level
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="text">Log message</param>
        private void WriteFormattedLog(LogLevel level, string text)
        {
            string pretext;
            switch (level)
            {
                case LogLevel.INFO: pretext = DateTime.Now.ToString(DatetimeFormat) + " [INFO] "; break;
                case LogLevel.WARNING: pretext = DateTime.Now.ToString(DatetimeFormat) + " [WARNING] "; break;
                case LogLevel.BROKER: pretext = DateTime.Now.ToString(DatetimeFormat) + " [IB INFO] "; break;
                case LogLevel.DB: pretext = DateTime.Now.ToString(DatetimeFormat) + " [DB INFO] "; break;
                case LogLevel.SIGNAL: pretext = DateTime.Now.ToString(DatetimeFormat) + " [SIGNAL INFO] "; break;
                case LogLevel.TRACE: pretext = DateTime.Now.ToString(DatetimeFormat) + " [TRACE] "; break;
                case LogLevel.DEBUG: pretext = DateTime.Now.ToString(DatetimeFormat) + " [DEBUG] "; break;                
                case LogLevel.ERROR: pretext = DateTime.Now.ToString(DatetimeFormat) + " [ERROR] "; break;
                case LogLevel.FATAL: pretext = DateTime.Now.ToString(DatetimeFormat) + " [FATAL] "; break;
                default: pretext = ""; break;
            }
            lock (_syncObject)
            {
                WriteLine(pretext + text);
                if (display) { Console.WriteLine(pretext + text); }
            }

        }

        /// <summary>
        /// Write a line of formatted log message into a log file
        /// </summary>
        /// <param name="text">Formatted log message</param>
        /// <param name="append">True to append, False to overwrite the file</param>
        /// <exception cref="System.IO.IOException"></exception>
        private void WriteLine(string text, bool append = true)
        {
            try
            {
                using (StreamWriter Writer = new StreamWriter(Filename, append, Encoding.UTF8))
                {
                    if (text != "") Writer.WriteLine(text);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.WriteLine(ex.StackTrace.ToString());
            }
        }
    }
}
