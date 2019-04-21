using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nog
{
    public class Logging
    {
        
        private string _loggingType;

        public Logging(string loggingType)
        {
            _loggingType = loggingType;
        }

        public void Debug(string message, string section = "")
        {
            var loggingType = (string.IsNullOrWhiteSpace(section)) ? _loggingType : _loggingType + "." + section;
            var logger = LogManager.GetLogger(loggingType);
            var logEvent = new LogEventInfo(LogLevel.Debug, loggingType, message);

            logger.Log(logEvent);
        }

        public void Info(string message, string section = "")
        {
            var loggingType = (string.IsNullOrWhiteSpace(section)) ? _loggingType : _loggingType + "." + section;
            var logger = LogManager.GetLogger(loggingType);
            var logEvent = new LogEventInfo(LogLevel.Info, loggingType, message);

            logger.Log(logEvent);
        }

        public void Error(Exception e)
        {
            var loggingType = _loggingType + ".error";
            var logger = LogManager.GetLogger(loggingType);
            var logEvent = new LogEventInfo();

            logEvent.Level = LogLevel.Error;
            logEvent.LoggerName = loggingType;
            logEvent.Exception = e;

            logger.Log(logEvent);
        }
    }
}
