using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Globalization;

public abstract class Connection
{
    Timer m_Timer = null;
    protected LogItemQueue  m_Logger;

    protected ServerCfg m_config = null;
    protected CTracker m_Tracker = null;

    bool        m_bRunning = false;
    int         m_LastFailTime = 0;

    public Connection(LogItemQueue logger, ServerCfg config)
    {
        m_Logger = logger;
        m_config = config;
        m_Logger.Push(LogLevel.WARNING, 0, "Client started");
        m_Timer = new Timer(new TimerCallback(Tick), null, 1000, 100);
    }
    protected bool DeletePoint(CDatabase db, TrackerPacketList pts)
    {
        try
        {
            using (MySqlCommand cmd = new MySqlCommand("UPDATE Points SET Send = Send | " + (1 << m_config.UID) + " WHERE TrackerID = ?TrackerID AND Time = ?Time", db.connection))
                using (MySqlTransaction trans = db.connection.BeginTransaction())
                {
                    cmd.Parameters.Add("?TrackerID", MySqlDbType.UInt32);
                    cmd.Parameters.Add("?Time", MySqlDbType.UInt32);
                    for (int i = 0; i < pts.Count; i++)
                    {
                        cmd.Parameters["?TrackerID"].Value = pts[i].m_ID;
                        cmd.Parameters["?Time"].Value = pts[i].m_Time;
                        cmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            for (int i = 0; i < pts.Count; i++)
            {
                decimal val = 0;
                pts[i].GetInput("ALARM", out val);
                m_config.SetAlarmState(m_Tracker, val > 0);
            }
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "Ошибка удаления точки " + ex.ToString());
        }
        return false;
    }
    protected TrackerPacketList GetPoint(CDatabase db)
    {
        TrackerPacket[] pt = new TrackerPacket[m_config.MaxPoints];
        try
        {
            TrackerPacketList points = new TrackerPacketList();
            if (points.Load(db, "SELECT  TrackerID, Time, Lat, Lng, Alt, Speed, Status, IO, GSMInfo FROM curpos WHERE TrackerID=" + m_Tracker.m_nID +
                            " order by Time desc LIMIT " + pt.Length))
                return points;
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "Error while points query " + ex.ToString());
        }
        return null;
    }

    protected void Tick(object obj)
    {
        int cnt=0;
        if (m_bRunning)
            return;

        m_bRunning = true;
        try
        {
            m_Tracker = m_config.GetNextTracker();
            if (m_Tracker == null)
            {
                m_bRunning = false;
                return;
            }
            TrackerPacketList pts;            
                DateTime start = DateTime.UtcNow;
                using (CDatabase db = Configuration.inst().GetDB()) { 
                     pts= GetPoint(db);
                if (pts != null && pts.Count > 0)
                {
                    DateTime query = DateTime.UtcNow;
                    DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    dt = dt.AddTicks(pts[0].m_Time * 10000000);
                    if(dt <= System.DateTime.UtcNow)
                    {                            
                     cnt= DoSession(pts);
                    }
                    DateTime delete = DateTime.UtcNow;
                    if(cnt == pts.Count && DeletePoint(db, pts))
                    {
                        m_Logger.Push(LogLevel.INFO, 0, cnt + " point sent of " + pts.Count +
                                        ".Times for query/session/delete:" + (query - start).TotalMilliseconds + "/" + (delete - query).TotalMilliseconds + "/" + (DateTime.UtcNow - delete).TotalMilliseconds);
                        m_LastFailTime = 0;
                    }
                    if (cnt == 0 && m_LastFailTime == 0)
                    {
                        m_LastFailTime = CTime.GetTime(DateTime.UtcNow);
                    }                    
                }
                insertRecord(db, pts); 
                }                                                                    
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, m_Tracker.m_IMEI  + " Error sending points " + ex.ToString());
            if (m_LastFailTime == 0)
                m_LastFailTime = CTime.GetTime(DateTime.UtcNow);
        }
        try
        {            
            if (m_LastFailTime > 0 && CTime.GetTime(DateTime.UtcNow) > m_LastFailTime + m_config.MaxFailTimeBeforeEmail)
            {
                m_LastFailTime = -1;
                EmailSender.inst().AddMessage(m_config.EmailAddr, m_config.EmailContent);
            }
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, m_Tracker.m_IMEI + " Error in connection " + ex.ToString());
        }        
        m_bRunning = false;
    }

    private void insertRecord(CDatabase db,TrackerPacketList pts)
    {
        if (pts != null)
        {
            string query = "";
            try
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    long devicetime = pts[i].m_Time;
                    long forwardTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    MySqlCommand mycom = db.connection.CreateCommand();
                    query = string.Concat("Update forwardrecords SET ForwardTimestamp='", forwardTime, "',RecSentDiff=(ForwardTimestamp-ReceivedTimestamp),TotalDifferance=(RecSentDiff+DevRecDiff) where DeviceTimestamp='", pts[i].m_Time, "' and ForwardTimestamp='0'");
                    mycom.CommandText = query;
                    if (!db.connection.State.Equals(System.Data.ConnectionState.Open))
                    {                        
                        db.connection.Open();
                    }                    
                    mycom.ExecuteNonQuery();
                    db.connection.Close();
                }
            }
            catch (Exception ex)
            {
                m_Logger.Push(LogLevel.ERROR, 0, query + "\n" + ex.Message + " " + ex.StackTrace);
            }
        }
    }
    protected void Close()
    {
        try
        {
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "Error " + ex.ToString());
        }
    }
    protected abstract int DoSession(TrackerPacketList pts);
}