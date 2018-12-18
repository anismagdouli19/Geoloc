using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Threading;
using GPSSender;

public class ServerCfg
{
    public int      UID = 0;
    public string   Type = "HTTP/JSON";
    public string   URL = "http://server.net";
    public int      MaxPoints = 100;
    public int      ThreadCount = 1;
    public int      Timeout = 10000;
    public bool     StaticDataResend= true;
    public string   EmailAddr = "";
    public string   EmailContent = "";
    public int      MaxFailTimeBeforeEmail = 60;

    public string   AlarmEmailAddr = "";
    public string   AlarmEmailContent = "alarm";

    CTrackerList    m_Trackers = new CTrackerList();
    int             m_nTracker = 0;
    Dictionary<int, int> m_Times = new Dictionary<int, int>();
    Timer           m_Timer = null;
    List<Connection> connections = new List<Connection>();
    string          m_strGroups = "";

    Dictionary<int, bool> m_TrackersAlarmState = new Dictionary<int, bool>();
    
    protected LogItemQueue m_Logger;

    public void Start(LogItemQueue log)
    {
        m_Logger = log;
        ServerGroupList list = Configuration.inst().ServerGroups.GetGroupsForServer(UID);
        for (int i = 0; i < list.Count; i++)
            m_strGroups += ((i > 0) ? "," : "") + "\"" + list[i].Name + "\"";

        if (list.Count > 0)
        {
            m_Timer = new Timer(new TimerCallback(Tick), null, 1000, 60000);

            for (int t = 0; t < ThreadCount; t++)
            {
                Connection con = null;
                if (Type == "HTTP/JSON")
                    con = new HttpJson(log, this);
                if (Type == "Wialon")
                    con = new WialonIPSConnection(log, this);
                if (Type == "Dingtek")
                    con = new DingtekConnection(log, this);
                if (Type == "DIMTS")
                    con = new DIMTSConnection(log, this);
                if (Type == "GpsGate")
                    con = new GpsGateConnection(log, this);
                if (Type == "Nagpur")
                    con = new NagpurConnection(log, this);
                if (con != null)
                    connections.Add(con);
            }
        }
    }
    protected void Tick(object obj)
    {
        try
        {
            using (CDatabase db = Configuration.inst().GetDB())
            {
                lock (m_Trackers)
                {
                    m_Trackers.Load(db, "SELECT * FROM Trackers WHERE servergroup in (" + m_strGroups + ")");
                }

                /*if (Configuration.inst().RemoveSendedPoints)
                {
                    string strQuery = "DELETE FROM Points WHERE TrackerID IN (SELECT ID FROM Trackers WHERE ServerGroup in(" + m_strGroups + ")) AND Send>= AND Send";
                    using (MySqlCommand cmd = new MySqlCommand(strQuery, db.connection))
                        cmd.ExecuteNonQuery();
                }*/
            }
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "Error " + ex.ToString());
        }
    }
    public CTracker GetNextTracker()
    {
        int now = CTime.GetTime(DateTime.UtcNow);
        lock (m_Trackers)
            if (m_Trackers.Count > 0)
                for (int i = 0; i < m_Trackers.Count; i++)
                {
                    m_nTracker = (m_nTracker + 1) % m_Trackers.Count;
                    if (!m_Times.ContainsKey(m_Trackers[m_nTracker].m_nID) || m_Times[m_Trackers[m_nTracker].m_nID] < now - 5)
                    {
                        m_Times[m_Trackers[m_nTracker].m_nID] = now;
                        return m_Trackers[m_nTracker];
                    }
                }
        return null;
    }
    public void SetAlarmState(CTracker tracker, bool state)
    {
        lock (m_TrackersAlarmState)
        {
            if (!m_TrackersAlarmState.ContainsKey(tracker.m_nID))
                m_TrackersAlarmState[tracker.m_nID] = false;
            
            if (state && !m_TrackersAlarmState[tracker.m_nID])
                EmailSender.inst().AddMessage(AlarmEmailAddr, tracker.m_strName + " (" + tracker.m_IMEI + ") " + AlarmEmailContent);

            m_TrackersAlarmState[tracker.m_nID] = state;
        }
    }
};

public class ServerList : List<ServerCfg>
{
}

public class ServerGroupCfg
{
    public string Name = "Group";
    public string Servers = "0 1";

    protected string[] ids = null;

    public bool ContainsServer(int uid)
    {
        if (ids == null)
            ids = Servers.Split(' ');

        for (int i = 0; i < ids.Length; i++)
            if (ids[i] == uid.ToString())
                return true;
        return false;
    }
};

public class ServerGroupList : List<ServerGroupCfg> 
{ 
    public ServerGroupList GetGroupsForServer(int uid)
    {
        ServerGroupList list = new ServerGroupList();
        for (int i = 0; i < Count; i++)
            if (this[i].ContainsServer(uid))
                list.Add(this[i]);
        return list;
    }
}

public class Configuration
{
    public class EmailConfig
    {
        public string SMTPHost = "";
        public int SMTPPort = 25;
        public bool SSL = false;
        public string User = "";
        public string Password = "";
        public string From = "";
        public string Title = "GPS Report";
    }

    public string SQLHost = "server5.locanix.net";
    public string SQLDB = "geoloc";
    public string SQLLogin = "L#xS5@55";
    public string SQLPassword = "true";

    public bool     AddDateToLog = false;
    //public bool     RemoveSendedPoints = false;

    public EmailConfig Email = new EmailConfig();

    public ServerList Servers = new ServerList();
    public ServerGroupList ServerGroups = new ServerGroupList();
    
    protected Configuration()
    {
    }

    private static Configuration _inst = null;
    public static Configuration inst()
    {
        if (_inst == null)
        {
            Configuration cfg = new Configuration();
            cfg.LoadConfig();
        }
        return _inst;
    }

    private bool LoadConfig()
    {
        try
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using (StreamReader SW = File.OpenText(path + "\\forwarder.xml"))
            {
                XmlSerializer s = new XmlSerializer(typeof(Configuration));
                _inst = (Configuration)s.Deserialize(SW);
            }

            //Console.WriteLine("Конфигурация успешно зачитана");
            return true;
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            _inst = this;
        }
        Console.WriteLine("Ошибка чтения конфигурации");
        return false;
    }

    public bool SaveConfig()
    {
        try
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using (StreamWriter SW = File.CreateText(path + "\\forwarder.xml"))
            {
                XmlSerializer s = new XmlSerializer(typeof(Configuration));
                s.Serialize(SW, this);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
        }
        return false;
    }

    public CDatabase GetDB()
    {
        return new CDatabase(SQLLogin, SQLPassword, SQLDB, SQLHost);
    }
}