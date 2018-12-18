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
    //////////////////////////////////////////////////////////////////////////
    public LogsCleaner(LogItemQueue logger)
    {
        m_Logger = logger;
        m_Timer = new Timer(Tick, null, 200000, 86400000);//раз в день
        m_Logger.Push(LogLevel.WARNING, 0, "LogsCleaner started");
    }
    //////////////////////////////////////////////////////////////////////////
    void Tick(object obj)
    {
        m_Logger.Push(LogLevel.INFO, 0, "Clearing DB......");
        try
        {
            DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using (CDatabase db = Configuration.inst().GetDB())
            {
                if (!db.IsConnected())
                {
                    m_Logger.Push(LogLevel.ERROR, 0, "Cant connect to db");
                    return;
                }

                DateTime dtNow = DateTime.Now;
                long now = (dtNow.ToFileTime() - dt1970.ToFileTime()) / 10000000;
                long time = now - 86400;
                using (MySqlCommand cmd = new MySqlCommand("UPDATE events SET status = status | 1 where status = 2 AND Time < " + time, db.connection))
                    cmd.ExecuteNonQuery();

                CTracker[] trackers = Global.m_ConfigMgr.GetTrackers();
                for (int i = 0; i < trackers.Length; i++ )
                {
                    time = now - trackers[i].m_nDaysToStore * 86400;

                    //удалить выполненные
                    Thread.Sleep(3000);
                    using (MySqlCommand cmd = 
                            new MySqlCommand("DELETE FROM Points WHERE TrackerID = " + trackers[i].m_nID + " AND Time < " + time + ";" +
                                             "DELETE FROM Events WHERE TrackerID = " + trackers[i].m_nID + " AND Time < " + time + ";", db.connection))
                        cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception e)
        {
            m_Logger.Push(LogLevel.ERROR, 0, e.ToString());
        }
        m_Logger.Push(LogLevel.INFO, 0, "Old data deleted");
    }
}