using System;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        public enum LogLevels
        {
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Fatal = 5
        }

        class Logger
        {
            public static readonly Logger PipeLoader = new Logger("PipeLoadErrors", LogLevels.Error);
            public static readonly Logger ContainerLoader = new Logger("ContainerLoadErrors", LogLevels.Error);
            public static readonly Logger FindErrors = new Logger("FindErrors", LogLevels.Error);
            public static readonly Logger Runtime = new Logger("Runtime", LogLevels.Info);

            public Logger(string filename, LogLevels defaultLogLevel)
            {
                _filename = filename;
                _defaultLogLevel = defaultLogLevel;
            }

            private readonly string _filename;
            private readonly LogLevels _defaultLogLevel;

            public void Log(string format, params object[] args)
            {
                Log(_defaultLogLevel, format, args);
            }
            public void Log(LogLevels logLevel, string format, params object[] args)
            {
                Instance.LogToFile(_filename, string.Format("[{0}]: {1}", logLevel, string.Format(format, args)), Instance);
            }

            public void LogSection(string section, string format, params object[] args)
            {
                LogSection(_defaultLogLevel, section, format, args);
            }
            public void LogSection(LogLevels logLevel, string section, string format, params object[] args)
            {
                Log(logLevel, "{0} - {1}", section, string.Format(format, args));
            }

            public void LogException(Exception e, string section = null)
            {
                Log(LogLevels.Error, e.Message);
                if(section != null)
                    Log(LogLevels.Error, "Exception thrown in {0}", section);
                Log(LogLevels.Error, e.Source);
                Log(LogLevels.Error, e.StackTrace);
                Instance.PrintError("Exception thrown. See log file for more details.");
            }
        }
    }
}
