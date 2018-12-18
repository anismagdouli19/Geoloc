using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Threading;

public class ConfigMgr
{
    LogItemQueue m_Logger;
    Timer m_ReloadConfigTimer;
    Dictionary<int, CTracker> m_TrackersByID = new Dictionary<int, CTracker>();
    Dictionary<long, CTracker> m_TrackersByIMEI = new Dictionary<long, CTracker>();

    Dictionary<int, CUser> m_Users = new Dictionary<int, CUser>();

    public ConfigMgr(LogItemQueue logger)
    {
        m_Logger = logger;
        m_ReloadConfigTimer = new Timer(new TimerCallback(ReloadConfig), null, 5 * 60000, 5 * 60000);

        ReloadConfig(null);

        m_Logger.Push(LogLevel.WARNING, 0, "ConfigMgr started");
    }
    ~ConfigMgr()
    {
        if (m_ReloadConfigTimer != null)
            m_ReloadConfigTimer.Dispose();
        m_TrackersByIMEI.Clear();
        m_TrackersByID.Clear();
    }
    private static ConfigMgr _inst = null;
    public static ConfigMgr inst()
    {
        if (_inst == null)
            _inst = new ConfigMgr(Global.m_Logger.GetQueue("ConfigMgr"));
        return _inst;
    }
    //////////////////////////////////////////////////////////////////////
    public CTrackerList GetTrackers()
    {
        CTrackerList trackers = new CTrackerList();
        lock (m_TrackersByID)
            foreach (CTracker tracker in m_TrackersByID.Values)
                trackers.Add(tracker);
        return trackers;
    }
    //////////////////////////////////////////////////////////////////////
    public CTracker GetTrackerByIMEI(long IMEI) 
    {
        CTracker tracker = null;
        lock (m_TrackersByIMEI)
            if (m_TrackersByIMEI.TryGetValue(IMEI, out tracker))
                return tracker;
        m_Logger.Push(LogLevel.WARNING, 0, "Invalid IMEI: " + IMEI);
        return null;
    }
    //////////////////////////////////////////////////////////////////////
    public CTracker GetTrackerByID(int ID)
    {
        CTracker tracker = null;
        lock (m_TrackersByID)
            if (m_TrackersByID.TryGetValue(ID, out tracker))
                return tracker;
        return null;
    }
    //////////////////////////////////////////////////////////////////////
    public CUser GetUser(int userID)
    {
        CUser user = null;
        lock (m_Users)
            m_Users.TryGetValue(userID, out user);
        return user;
    }
    public CUserList GetUsers()
    {
        CUserList users = new CUserList();
        lock (m_Users)
            foreach (CUser user in m_Users.Values)
                users.Add(user);
        return users;
    }
    //////////////////////////////////////////////////////////////////////
    void ReloadConfig(object obj)//зачитываем данные из БД
    {
        CTrackerList trackers = new CTrackerList();

        Dictionary<long, CTracker> trackersByIMEI = new Dictionary<long, CTracker>();

        List<ulong> zones2users = new List<ulong>();
        //List<ulong> trackers2users = new List<ulong>();

        CUserList users = new CUserList();


        using (CDatabase db = Configuration.inst().GetDB())
            if (db.IsConnected())
            {
                if (!trackers.Load(db))
                    m_Logger.Push(LogLevel.ERROR, 0, "Ошибка загрузки списка трекеров");

                users.Load(db);
                ////////////////////////////////////////////////////////////////////////////////////////
                try
                {
                    //вставим точки для трекеров в curpos
                    using (MySqlCommand cmd = new MySqlCommand("INSERT IGNORE INTO curpos(TrackerID) SELECT ID FROM Trackers", db.connection))
                        cmd.ExecuteNonQuery();
                    //вставим точки для трекеров в lastpoint
                    using (MySqlCommand cmd = new MySqlCommand("INSERT IGNORE INTO lastpoint(TrackerID) SELECT ID FROM Trackers", db.connection))
                        cmd.ExecuteNonQuery();
                }
                catch (Exception ex) 
                { 
                    m_Logger.Push(LogLevel.ERROR, 0, ex.ToString()); 
                }
            }
            else
                m_Logger.Push(LogLevel.ERROR, 0, "Не удалось соединиться с БД");

        ////////////////////////////////////////////////////////////////////////////////////////
        lock (m_TrackersByIMEI)
            lock (m_TrackersByID)
            {
                m_TrackersByIMEI.Clear();
                m_TrackersByID.Clear();

                for (int i = 0; i < trackers.Count; i++)
                {
                    m_TrackersByIMEI[trackers[i].m_IMEI] = trackers[i];
                    m_TrackersByID[trackers[i].m_nID] = trackers[i];
                }
            }
        /*lock (m_TrackersByUserID)
        {
            m_TrackersByUserID.Clear();
            for (int i = 0; i < trackers.Count; i++)
                m_TrackersByUserID.Add(trackers[i].m_nUserID, trackers[i]);

            for (int i = 0; i < trackers2users.Count; i++)
            {
                int ID = (int)(trackers2users[i] & 0xFFFFFFFF);
                int userID = (int)(trackers2users[i] >> 32);

                CTracker tracker = GetTrackerByID(ID);
                if (tracker != null)
                    m_TrackersByUserID.Add(userID, tracker);
            }
        }*/
        ////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////
        lock (m_Users)
        {
            m_Users.Clear();
            for (int i = 0; i < users.Count; i++)
                m_Users[users[i].m_nID] = users[i];
        }
    }
}
