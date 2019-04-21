/*-------------------------------------------------------
'	프로그램명	: Forlder Detect
'	작성자		: DevOpsFlux
'	작성일		: 2015-06-20
'	설명		: MultiWatcher Class
'   http://msdn.microsoft.com/en-us/library/system.io.filesystemeventargs(v=vs.100).aspx
' -------------------------------------------------------*/
using System;
using System.IO;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Json;
using System.Text;
using Nog;
using NLog;

namespace MultiWatcher
// ConsoleApplication, which monitors TXT-files in multiple folders. 
// Inspired by:
// http://msdn.microsoft.com/en-us/library/system.io.filesystemeventargs(v=vs.100).aspx

{
    public class Watchers
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

        private static string hostName = ConfigurationManager.AppSettings["HostName"].ToString();
        public static int FileWatchNum { get; set; }

        private static Dictionary<string, WatcherChangeTypes> BeforeFile = new Dictionary<string, WatcherChangeTypes>();

        public static void Main()
        {
            Run();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void Run()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            string SD = ConfigurationManager.AppSettings["SearchDrive"].ToString();

            string[] DriveList = SD.Split('|');

            foreach(var item in DriveList)
            {
                Watch(item);
            }

            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;
        }
        private static void Watch(string watch_folder)
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = watch_folder;

            // watcher가 하위 폴더까지 검색할수있는 권한 설정
            watcher.IncludeSubdirectories = true;

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName;

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            
            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        private static bool ExceptionWord(FileSystemEventArgs e)
        {
            string ExceptString = ConfigurationManager.AppSettings["ExceptionWord"].ToString();

            string[] WordList = ExceptString.Split('|');

            foreach (var item in WordList)
            {
                if (e.FullPath.ToLower().IndexOf(item) != -1) return false;
            }

            return true;
        }

        private static void FileWatchNumCheck()
        {
            FileWatchNum++;

            if (FileWatchNum >= 100)
            {
                Console.Clear();
                FileWatchNum = 0;
            }
        }

        private static string CreateFolder(string sourceFolder, string destFolder)
        {
            try
            {
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);

                // 복사할 경로의 root경로만 옮겨질 경로의 폴더경로로 변경하여 폴더 생성
                string root = Directory.GetDirectoryRoot(sourceFolder).ToString();
                string targetPath = sourceFolder.Replace(root, destFolder + "\\");

                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                return targetPath;
            }
            catch (Exception ex)
            {
                return "X";
            }
        }

        private static void CreateFile(string FilePath, string status)
        {
            string DirPath = ConfigurationManager.AppSettings["CopyFolder"].ToString(); ;
            DirectoryInfo di = new DirectoryInfo(DirPath);

            string TargetPath = "";
            string FileName = Path.GetFileName(FilePath);

            try
            {
                if (di.Exists != true) Directory.CreateDirectory(DirPath);

                if (status.Equals("Move"))
                {
                    TargetPath = CreateFolder(Path.GetDirectoryName(FilePath), DirPath);
                    if (TargetPath.Equals("X"))
                        return;
                    System.IO.File.Move(FilePath, Path.Combine(TargetPath, FileName));
                }
                else if (status.Equals("Copy"))
                {
                    TargetPath = CreateFolder(Path.GetDirectoryName(FilePath), DirPath);
                    if (TargetPath.Equals("X"))
                        return;
                    System.IO.File.Copy(FilePath, Path.Combine(TargetPath, FileName), true);
                }
                else
                {
                    return;
                }


            }
            catch (Exception e)
            {

            }
        }

        private static string LogMessage(MessageType messagetype, LogType logtype, FileSystemEventArgs fse, RenamedEventArgs re = null)
        {
            switch (logtype)
            {
                case Watchers.LogType.CONSOLE:
                    if (messagetype == MessageType.ONCHANGE)
                        return string.Format("[보안] {3} - File: {0} {1} {2}", fse.FullPath, fse.ChangeType, DateTime.Now, hostName);
                    else
                        return string.Format("[보안] {3} - File: {0} renamed to {1} DATA : {2}", re.OldFullPath, re.FullPath, DateTime.Now, hostName);
                case Watchers.LogType.FILE:
                    if (messagetype == MessageType.ONCHANGE)
                        return string.Format("[보안] {0} - File: {1}", hostName, fse.FullPath);
                    else
                        return string.Format("[보안] {0} - File: {1} Rename to {2}", hostName, re.FullPath, re.OldFullPath);
                case Watchers.LogType.SMS:
                    if (messagetype == MessageType.ONCHANGE)
                        return string.Format("[보안] File({0}) was {1}. Time {2} [{3}]", fse.FullPath, fse.ChangeType, DateTime.Now, hostName);
                    else
                        return string.Format("[보안] File({0} Rename to {1}) was {2}. Time {3} [{4}]", re.FullPath, re.OldFullPath, re.ChangeType, DateTime.Now, hostName);
                default:
                    return "??????????";
            }
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            string CreateStatus = ConfigurationManager.AppSettings["CreateStatus"].ToString();

            if (ExceptionWord(e) == false) return;

            // 이벤트가 created일 경우 발생 
            if (e.ChangeType.ToString().ToLower().Equals("created"))
            {
                CreateFile(e.FullPath, CreateStatus);
            }

            Console.WriteLine(LogMessage(MessageType.ONCHANGE, LogType.CONSOLE, e));

            FileWatchNumCheck();
            
            WriteNlog(LogMessage(MessageType.ONCHANGE, LogType.FILE, e), e.ChangeType);

            //SMS 전송
            DirectoryInfo di = new DirectoryInfo(e.FullPath);

            //풀네임이 검색되지않았다는것은 파일이라는 뜻
            if(di.Exists == false)
            {
                SendSms(LogMessage(MessageType.ONCHANGE, LogType.SMS, e));
            }
            else
            {
                if(e.ChangeType != WatcherChangeTypes.Changed)
                {
                    SendSms(LogMessage(MessageType.ONCHANGE, LogType.SMS, e));
                }
            }
        }
       
        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            if (ExceptionWord(e) == false) return;

            Console.WriteLine(LogMessage(MessageType.ONRENAME, LogType.CONSOLE, null, e));
            
            FileWatchNumCheck();

            WriteNlog(LogMessage(MessageType.ONRENAME, LogType.FILE, null, e), e.ChangeType);

            //SMS 전송
            SendSms(LogMessage(MessageType.ONRENAME, LogType.SMS, null, e));
        }


        #region Log
        private static void Log(string str)
        {
            string FilePath = @"D:\FileWatcherLog\Log.txt";
            string DirPath = @"D:\FileWatcherLog";

            DirectoryInfo di = new DirectoryInfo(DirPath);
            FileInfo fi = new FileInfo(FilePath);

            try
            {
                if (di.Exists != true) Directory.CreateDirectory(DirPath);

                if (fi.Exists != true)
                {
                    using (StreamWriter sw = new StreamWriter(FilePath))
                    {

                        sw.WriteLine(str);
                        sw.Close();
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(FilePath))
                    {

                        sw.WriteLine(str);
                        sw.Close();
                    }
                }
            }
            catch (Exception e)
            {
                
            }
        }


        private static void WriteNlog(string log, WatcherChangeTypes e)
        {
            var logger = new Logging(e.ToString());

            logger.Info(log);
        }
        #endregion

        #region SMS
        private static void SendSms(string msg)
        {
            // 디버그 모드시에 SMS 전송 안됨
            string isDebug = ConfigurationManager.AppSettings["Debug"].ToString();
            if (isDebug.Equals("Y")) return;

            string Phone = ConfigurationManager.AppSettings["PhoneNum"].ToString();

            string[] PhoneList = Phone.Split('|');

            foreach (var item in PhoneList)
            {
                Sms(msg, item);
            }
        }

        private static void Sms(string msg,string phone)
        {

            string SMSKey = ConfigurationManager.AppSettings["SMSKey"].ToString();
            string PartCode = ConfigurationManager.AppSettings["PartCode"].ToString();
            string SMSUrl = ConfigurationManager.AppSettings["SMSUrl"].ToString();
            string CallBackNum = ConfigurationManager.AppSettings["CallBackNum"].ToString();

            //json 값 생성 
            JsonObjectCollection res = new JsonObjectCollection();
            res.Add(new JsonStringValue("callbackNo", CallBackNum));
            res.Add(new JsonStringValue("message", msg));
            res.Add(new JsonStringValue("phoneNo", phone));
            res.Add(new JsonStringValue("reservationNo", ""));
            res.Add(new JsonStringValue("subject", "[보안] 파일 변경 감지"));
            res.Add(new JsonStringValue("systemCD", PartCode));


            // 전송할 uri
            WebRequest request = WebRequest.Create(SMSUrl);

            // post 전송할 헤드 설정
            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(res.ToString());
            request.ContentType = " text/json";
            // 서버 키 입렵
            request.Headers.Add("openapikey", SMSKey);
            request.ContentLength = byteArray.Length;

            // stream으로 변환한뒤 json 전송
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            Console.WriteLine(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();

        }
        #endregion
    }
}