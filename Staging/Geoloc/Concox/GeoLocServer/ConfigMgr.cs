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
    Dictionary<long, CTracker> m_TrackersByID = new Dictionary<long, CTracker>();
    Dictionary<long, CTracker> m_TrackersByIMEI = new Dictionary<long, CTracker>();

    public ConfigMgr(LogItemQueue logger)
    {
        m_Logger = logger;
        m_ReloadConfigTimer = new Timer(new TimerCallback(ReloadConfig), null, 2000, 5 * 60000);

        m_Logger.Push(LogLevel.WARNING, 0, "ConfigMgr started");
    }
    ~ConfigMgr()
    {
        if (m_ReloadConfigTimer != null)
            m_ReloadConfigTimer.Dispose();
        m_TrackersByIMEI.Clear();
        m_TrackersByID.Clear();
    }
    //////////////////////////////////////////////////////////////////////
    public CTracker[] GetTrackers()
    {
        CTracker[] trackers = null;
        lock (m_TrackersByID)
        {
            trackers = new CTracker[m_TrackersByID.Count];
            int count = 0;
            foreach (CTracker tracker in m_TrackersByID.Values)
                trackers[count++] = tracker;
        }
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
    public CTracker GetTrackerByID(long ID)
    {
        CTracker tracker = null;
        lock (m_TrackersByID)
            if (m_TrackersByID.TryGetValue(ID, out tracker))
                return tracker;
        return null;
    }
    //////////////////////////////////////////////////////////////////////
    void ReloadConfig(object obj)//зачитываем данные из Ѕƒ
    {
        Dictionary<long, CTracker> trackersByIMEI = new Dictionary<long, CTracker>();

        using (CDatabase db = Configuration.inst().GetDB())
            if (db.IsConnected())
            {
                CTrackerList trackers = new CTrackerList();
                if (!trackers.Load(db))
                    m_Logger.Push(LogLevel.ERROR, 0, "Error loading trackers list");

                for (int i = 0; i < trackers.Count; i++)
                    trackersByIMEI[trackers[i].m_IMEI] = trackers[i];

                ////////////////////////////////////////////////////////////////////////////////////////
                try
                {
                    //вставим точки дл€ трекеров в curpos
                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO curpos(TrackerID) SELECT ID FROM Trackers ON DUPLICATE KEY UPDATE curpos.TrackerID=Trackers.ID", db.connection))
                        cmd.ExecuteNonQuery();
                    //вставим точки дл€ трекеров в lastpoint
                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO lastpoint(TrackerID) SELECT ID FROM Trackers ON DUPLICATE KEY UPDATE lastpoint.TrackerID=Trackers.ID", db.connection))
                        cmd.ExecuteNonQuery();
                }
                catch (Exception ex) 
                { 
                    m_Logger.Push(LogLevel.ERROR, 0, ex.ToString()); 
                }
            }
            else
                m_Logger.Push(LogLevel.ERROR, 0, "Cant connect to db");

        ////////////////////////////////////////////////////////////////////////////////////////
        lock (m_TrackersByIMEI)
            lock (m_TrackersByID)
            {
                m_TrackersByIMEI.Clear();
                m_TrackersByID.Clear();

                foreach (CTracker tracker in trackersByIMEI.Values)
                {
                    m_TrackersByIMEI[tracker.m_IMEI] = tracker;
                    m_TrackersByID[tracker.m_nID] = tracker;
                }
            }
    }
}
