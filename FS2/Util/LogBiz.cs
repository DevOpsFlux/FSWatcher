using Nog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS2.Util.Biz
{
    enum LogType
    {
        CONSOLE = 0,
        FILE,
        SMS
    }

    enum MessageType
    {
        ONCHANGE = 0,
        ONRENAME
    }

    class LogBiz
    {
        public string LogMessage(MessageType messagetype, LogType logtype, string hostname, FileSystemEventArgs fse, RenamedEventArgs re = null)
        {
            switch (logtype)
            {
                case LogType.CONSOLE:
                    if (messagetype == MessageType.ONCHANGE)
                        return string.Format("[보안] {3} - File: {0} {1} {2}", fse.FullPath, fse.ChangeType, DateTime.Now, hostname);
                    else
                        return string.Format("[보안] {3} - File: {0} renamed to {1} DATA : {2}", re.OldFullPath, re.FullPath, DateTime.Now, hostname);
                case LogType.FILE:
                    if (messagetype == MessageType.ONCHANGE)
                        return string.Format("[보안] {0} - File: {1}", hostname, fse.FullPath);
                    else
                        return string.Format("[보안] {0} - File: {1} Rename to {2}", hostname, re.FullPath, re.OldFullPath);
                case LogType.SMS:
                    if (messagetype == MessageType.ONCHANGE)
                        return string.Format("[보안] File({0}) was {1}. Time {2} [{3}]", fse.FullPath, fse.ChangeType, DateTime.Now, hostname);
                    else
                        return string.Format("[보안] File({0} Rename to {1}) was {2}. Time {3} [{4}]", re.FullPath, re.OldFullPath, re.ChangeType, DateTime.Now, hostname);
                default:
                    return "??????????";
            }
        }

        public void WriteNlog(string log, WatcherChangeTypes e)
        {
            var logger = new Logging(e.ToString());

            logger.Info(log);
        }
    }
}
