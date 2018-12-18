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
    bool  m_bRunning = false;
    int   m_LastFailTime = 0;
    public Connection(LogItemQueue logger, ServerCfg config)
    {
        m_Logger = logger;
        m_config = config;
        m_Logger.Push(LogLevel.WARNING, 0, "Client started");
        m_Timer = new Timer(new TimerCallback(Tick), null,1000, 100);
    }
    protected bool DeletePoint(CDatabase db, TrackerPacketList pts)
    {
        try
        {
            using (MySqlCommand cmd = new MySqlCommand("UPDATE Points SET Send = Send | " + (1 << m_config.UID) + " ,ForwardTime=case when ForwardTime=0 then UNIX_TIMESTAMP(now()) else ForwardTime end,NTS=NTS+1,VehicleStatus=case when speed>0 then 1 else 0 end WHERE TrackerID = ?TrackerID AND Time = ?Time", db.connection))
            {                
                using (MySqlTransaction trans = db.connection.BeginTransaction())
                {
                    try { 
                    cmd.Parameters.Add("?TrackerID", MySqlDbType.UInt64);
                    cmd.Parameters.Add("?Time", MySqlDbType.UInt32);                    
                    for (int i = 0; i < pts.Count; i++)
                    {
                        cmd.Parameters["?TrackerID"].Value = pts[i].m_ID;
                        cmd.Parameters["?Time"].Value = pts[i].m_Time;                        
                        cmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                    }
                    catch(Exception ex)
                    {
                        trans.Rollback();
                    }                    
                }                
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
            m_Logger.Push(LogLevel.ERROR, 0, "Unable To send Data " + ex.Message+" "+ex.StackTrace);
        }
        return false;
    }
    protected TrackerPacketList GetPoint(CDatabase db)
    {
        TrackerPacket[] pt = new TrackerPacket[m_config.MaxPoints];
        try
        {

            TrackerPacketList points = new TrackerPacketList();
            if (points.Load(db, "SELECT  TrackerID, Time, Lat, Lng,Alt,Speed,Status,IO,GSMInfo FROM lastpoint WHERE TrackerID=" + m_Tracker.m_nID + " and Time>0 and Time<=UNIX_TIMESTAMP(now()) and Lat>0" + pt.Length))
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
                    
                    DateTime dt = CTime.GetTime(Convert.ToInt64(pts[0].m_Time));
                    if (dt <= DateTime.UtcNow)
                    {
                        if (((System.DateTime.UtcNow.AddMinutes(-3)) > dt && pts[0].m_Speed == 0) && m_config.StaticDataResend)
                        { 
                            cnt= DoSession(pts,1);
                        }
                        else 
                        {
                            if (dt > (System.DateTime.UtcNow.AddMinutes(-3)))
                            {
                            cnt= DoSession(pts,0);
                            }
                        }                        
                    }
                    else
                    {
                        EmailSender.inst().AddMessage(m_config.EmailAddr,pts[0].m_ID+" Is Sending Future Time Stamp Data");
                    }
                    if (System.DateTime.Now >System.DateTime.UtcNow.AddHours(5).AddMinutes(30))
                    {
                        EmailSender.inst().AddMessage(m_config.EmailAddr,System.Net.Dns.GetHostName() +" Has Wrong Date Time Setting");
                    }
                    DateTime delete = DateTime.UtcNow;
                    if(cnt == pts.Count && DeletePoint(db, pts))
                    {
                        m_Logger.Push(LogLevel.INFO, 0, cnt + " point sent of " + pts.Count +
                                        ".Times for query/session/delete:" + (DateTime.UtcNow - start).TotalMilliseconds + "/" + (delete - DateTime.UtcNow).TotalMilliseconds + "/" + (DateTime.UtcNow - delete).TotalMilliseconds);
                        m_LastFailTime = 0;
                    }
                    if (cnt == 0 && m_LastFailTime == 0)
                    {
                        m_LastFailTime = CTime.GetTime(DateTime.UtcNow);
                    }                    
                }                
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
    protected abstract int DoSession(TrackerPacketList pts,int isResend);
}