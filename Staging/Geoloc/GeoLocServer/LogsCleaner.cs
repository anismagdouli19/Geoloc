using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;

class LogsCleaner
{
    LogItemQueue m_Logger;
    Timer m_Timer = null;

    CTrackerList m_Trackers = new CTrackerList();
    int m_LastTrackerListLoadTime = 0;
    int m_nTrackerIndex = 0;

    bool m_bRunning = false;
    //////////////////////////////////////////////////////////////////////////
    public LogsCleaner(LogItemQueue logger)
    {
        m_Logger = logger;
        m_Timer = new Timer(Tick, null, 200000, 86400000);
        m_Logger.Push(LogLevel.WARNING, 0, "LogsCleaner started");
    }
    //////////////////////////////////////////////////////////////////////////
    void Tick(object obj)
    {
        if (m_bRunning)
            return;
        m_bRunning = true;

        int now = CTime.GetTime(DateTime.UtcNow);
        if (m_LastTrackerListLoadTime < now - 12 * 3600 && m_nTrackerIndex >= m_Trackers.Count)
        {
            m_Trackers = ConfigMgr.inst().GetTrackers();
            m_LastTrackerListLoadTime = now;
            m_nTrackerIndex = 0;

            ClearCommonLogs();
            int t = CTime.GetTime(DateTime.UtcNow);
            if (t - now > 1)
                m_Logger.Push(LogLevel.WARNING, 0, "Common data cleared for " + (t - now) + " sec");
        }

        now = CTime.GetTime(DateTime.UtcNow);
        if (m_nTrackerIndex < m_Trackers.Count)
        {
            CTracker tracker = m_Trackers[m_nTrackerIndex++];
            ClearLogsForTracker(tracker);

            int t = CTime.GetTime(DateTime.UtcNow);
            if (t - now > 1)
                m_Logger.Push(LogLevel.WARNING, 0, "Data for " + tracker.GetDesc() + " cleared for " + (t-now) + " sec");
        }

        m_bRunning = false;
    }
    void ClearLogsForTracker(CTracker tracker)
    {
        int now = CTime.GetTime(DateTime.UtcNow);

        try
        {
            using (CDatabase db = Configuration.inst().GetDB())
            {
                if (!db.IsConnected())
                {
                    m_Logger.Push(LogLevel.ERROR, 0, "Не удалось соединиться с БД");
                    return;
                }

                int storedatatime = now - tracker.m_nDaysToStore * 86400;

                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM Points WHERE TrackerID = " + tracker.m_nID + " AND Time < " + storedatatime, db.connection))
                    cmd.ExecuteNonQuery();

                /*string types = "";
                int storeunusedeventstime = now - 10 * 86400;
                for (int i = 0; i < CEventList.UsefulEvents.Length; i++)
                    types += ((i > 0) ? "," : "") + (int)CEventList.UsefulEvents[i];*/
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM Events WHERE TrackerID = " + tracker.m_nID + " AND Time < " + storedatatime + " AND Status=3", db.connection))
                    cmd.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            m_Logger.Push(LogLevel.ERROR, 0, e.ToString());
        }
    }
    void ClearCommonLogs()
    {
        try
        {
            using (CDatabase db = Configuration.inst().GetDB())
            {
                if (!db.IsConnected())
                {
                    m_Logger.Push(LogLevel.ERROR, 0, "Не удалось соединиться с БД");
                    return;
                }

                int time = CTime.GetTime(DateTime.UtcNow) - 86400;
                using (MySqlCommand cmd = new MySqlCommand("UPDATE events SET status = status | 1 where status = 2 AND Time < " + time, db.connection))
                    cmd.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            m_Logger.Push(LogLevel.ERROR, 0, e.ToString());
        }
    }
}
