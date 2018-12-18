using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

public enum LogLevel
{
    DEBUG = 0,
    INFO,
    WARNING,
    ERROR
};
public struct LogItem
{
    public DateTime     m_Time;
    public LogLevel     m_nLogLevel;
    public uint         m_nSourceID;
    public string       m_strMessage;

    public LogItem(LogLevel loglevel, uint nSourceID, string strMessage)
    {
        m_nLogLevel = loglevel;
        m_nSourceID = nSourceID;
        m_Time = DateTime.Now;
        m_strMessage = strMessage;//.Replace(",", ".");

        Console.WriteLine(ToString());

        if (m_nLogLevel == LogLevel.ERROR)
            Debug.Assert(false, m_strMessage);
    }

    public override string ToString()
    {
        return m_Time.ToString("dd.MM.yyyy HH:mm:ss") + "; " + m_nLogLevel + "; " + m_nSourceID + "; " + m_strMessage;

    }
}
public class LogItemQueue: Queue<LogItem>
{
    public string m_strFileName = "";

    public LogItemQueue(string strFileName)
    {
        m_strFileName = strFileName;
    }

    public void Push(LogLevel loglevel, uint nSourceID, string strMessage)
    {
        #if !DEBUG
        if (loglevel > LogLevel.DEBUG)
        #endif
            lock (this)
                Enqueue(new LogItem(loglevel, nSourceID, strMessage));
    }
}

public class Logger
{
    Dictionary<string, LogItemQueue> m_Files = new Dictionary<string, LogItemQueue>();
    Timer m_FlushDataTimer;
    string m_strLogDir = "log/";
    ////////////////////////////////////////////////////////////////////////////////////
    public Logger()
    {
        try
        {
            m_strLogDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/log/"; 
        }
        catch (Exception ex) { ex = null; }
        m_FlushDataTimer = new Timer(new TimerCallback(FlushData), null, 5000, 10000);
    }
    ////////////////////////////////////////////////////////////////////////////////////
    public LogItemQueue GetQueue(string filename)
    {
        LogItemQueue queue = null;
        lock (m_Files)
        {
            if (!m_Files.ContainsKey(filename))
                m_Files.Add(filename, new LogItemQueue(filename + ".log"));
            queue = m_Files[filename];
        }
        return queue;
    }
    ////////////////////////////////////////////////////////////////////////////////////
    public void FlushData()
    {
        FlushData(this);
    }
    ////////////////////////////////////////////////////////////////////////////////////
    void FlushData(object me)    //записываем данные в базу данных
    {
        DateTime dtNow = DateTime.Now;
        string strLogDir = m_strLogDir;
        try
        {
            Directory.CreateDirectory(m_strLogDir, null);
            if (Configuration.inst().AddDateToLog)
            {
                strLogDir = m_strLogDir + dtNow.ToString("yyyy.MM.dd").Replace(".", "/") + "/";
                Directory.CreateDirectory(strLogDir, null);
            }
        }
        catch (Exception ex) { }

        try
        {
            lock (m_Files)
            foreach (LogItemQueue queue in m_Files.Values)
                lock (queue)
                {
                    if (queue.Count > 2000)
                        queue.Clear();

                    if (queue.Count > 0)
                    {
                        try
                        {
                            using (StreamWriter SW = File.AppendText(strLogDir + queue.m_strFileName))
                            {
                                while (queue.Count > 0)
                                {
                                    LogItem item = queue.Dequeue();
                                    string str = item.ToString();
                                    SW.WriteLine(str);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                            Debug.Assert(false, e.ToString());
                        }
                    }
                }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            Debug.Assert(false, e.ToString());
        }
    }
    ////////////////////////////////////////////////////////////////////////////////////
}
