using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Threading;
using System.Collections;
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
/// <summary>
/// очередь, логирующая данные от трекеров в БД
/// </summary>
//////////////////////////////////////////////////////////////////////
class GPSPointQueue
{
    LogItemQueue m_Logger;
    Queue<TrackerPacket>   m_Queue = new Queue<TrackerPacket>();           //очередь данных
    Dictionary<uint, long> m_SessionTrackers = new Dictionary<uint, long>();

    Timer       m_FlushDataTimer;
    bool        m_bFlushing = false;
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    public GPSPointQueue(LogItemQueue logger)
    {
        m_Logger = logger;
        m_FlushDataTimer = new Timer(new TimerCallback(FlushData), null, 20000, 10000);

        m_Logger.Push(LogLevel.WARNING, 0, "GPSPointQueue started");
    }
    //////////////////////////////////////////////////////////////////////
    ~GPSPointQueue()
    {
        if (m_FlushDataTimer != null)
            m_FlushDataTimer.Dispose();
    }
    //////////////////////////////////////////////////////////////////////
    public bool PushPacket(TrackerPacket packet)
    {
        try
        {
            CTracker tracker = Global.m_ConfigMgr.GetTrackerByIMEI(packet.m_ID);
            if (tracker != null)
            {
                packet.m_ID = tracker.m_nID;
                if (packet.IsValid())
                    lock (m_Queue)
                        m_Queue.Enqueue(packet);
                return true;
            }
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "PushPacket:" + ex.ToString());
        }
        return false;
    }
    //////////////////////////////////////////////////////////////////////
    public bool PushPacketWithID(TrackerPacket packet)
    {
        try
        {
            CTracker tracker = Global.m_ConfigMgr.GetTrackerByID(packet.m_ID);
            if (tracker != null)
            {
                if (packet.IsValid())
                    lock (m_Queue)
                        m_Queue.Enqueue(packet);
                return true;
            }
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "PushPacket:" + ex.ToString());
        }
        return false;
    }
    //////////////////////////////////////////////////////////////////////
    public bool SetOnline(long IMEI, uint sessid)
    {
        CTracker tracker = Global.m_ConfigMgr.GetTrackerByIMEI(IMEI);
        if (tracker != null)
            lock (m_SessionTrackers)
                m_SessionTrackers[sessid] = tracker.m_nID;
        return tracker != null;
    }
    //////////////////////////////////////////////////////////////////////
    public void SetOffline(uint sessid)
    {
        lock (m_SessionTrackers)
            m_SessionTrackers.Remove(sessid);
    }
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    void FlushData(object obj)    //записываем данные в базу данных
    {
        if (m_bFlushing)
            return;
        m_bFlushing = true;

        //записать в базу все точки
        lock (m_Queue)
            if (m_Queue.Count > 100000)
            {
                m_Queue.Clear();
                m_Logger.Push(LogLevel.ERROR, 0, "Points queue purged");
            }

       
        using (CDatabase db = Configuration.inst().GetDB())
        {
            if (!db.IsConnected())
            {
                m_bFlushing = false;
                m_Logger.Push(LogLevel.ERROR, 0, "Cant connect to db");
                return;
            }

            if (m_Queue.Count > 0)
            try
            {
                using (MySqlCommand cmdUpdate = new MySqlCommand("UPDATE curpos SET Time=?Time, Lat=?Lat, Lng=?Lng, Status=?Status, Speed=?Speed, Alt=?Alt, IO=?IO, GSMInfo=?GSMInfo WHERE TrackerID = ?ID AND Time <= ?Time", db.connection))
                using (MySqlCommand cmdUpdate1 = new MySqlCommand("UPDATE lastpoint SET Time=?Time, Lat=?Lat, Lng=?Lng, Status=?Status, Speed=?Speed, Alt=?Alt, IO=?IO, GSMInfo=?GSMInfo WHERE TrackerID = ?ID AND Time <= ?Time", db.connection))
                using (MySqlCommand cmdInsert = new MySqlCommand("INSERT INTO points(TrackerID, Time, Lat, Lng, Status, Speed, Alt, IO, GSMInfo,ForwardTime,ReceivedTime) VALUES(?ID, ?Time, ?Lat, ?Lng, ?Status, ?Speed, ?Alt, ?IO, ?GSMInfo,0," + CTime.GetTime(DateTime.UtcNow) + ") ON DUPLICATE KEY UPDATE TrackerID=TrackerID", db.connection))
                {
                    cmdUpdate.Parameters.Add("?ID", MySqlDbType.Int32);     cmdInsert.Parameters.Add("?ID", MySqlDbType.Int32);
                    cmdUpdate.Parameters.Add("?Time", MySqlDbType.Int32);   cmdInsert.Parameters.Add("?Time", MySqlDbType.Int32);
                    cmdUpdate.Parameters.Add("?Lat", MySqlDbType.Float);    cmdInsert.Parameters.Add("?Lat", MySqlDbType.Float);
                    cmdUpdate.Parameters.Add("?Lng", MySqlDbType.Float);    cmdInsert.Parameters.Add("?Lng", MySqlDbType.Float);
                    cmdUpdate.Parameters.Add("?Alt", MySqlDbType.Int32);    cmdInsert.Parameters.Add("?Alt", MySqlDbType.Int32);
                    cmdUpdate.Parameters.Add("?Speed", MySqlDbType.Int32);  cmdInsert.Parameters.Add("?Speed", MySqlDbType.Int32);
                    cmdUpdate.Parameters.Add("?Status", MySqlDbType.Int32); cmdInsert.Parameters.Add("?Status", MySqlDbType.Int32);
                    cmdUpdate.Parameters.Add("?IO", MySqlDbType.Blob);      cmdInsert.Parameters.Add("?IO", MySqlDbType.Blob);
                    cmdUpdate.Parameters.Add("?GSMInfo", MySqlDbType.UInt64); cmdInsert.Parameters.Add("?GSMInfo", MySqlDbType.UInt64);
                    cmdUpdate1.Parameters.Add("?ID", MySqlDbType.Int32); 
                    cmdUpdate1.Parameters.Add("?Time", MySqlDbType.Int32);
                    cmdUpdate1.Parameters.Add("?Lat", MySqlDbType.Float);
                    cmdUpdate1.Parameters.Add("?Lng", MySqlDbType.Float);
                    cmdUpdate1.Parameters.Add("?Alt", MySqlDbType.Int32);
                    cmdUpdate1.Parameters.Add("?Speed", MySqlDbType.Int32);
                    cmdUpdate1.Parameters.Add("?Status", MySqlDbType.Int32);
                    cmdUpdate1.Parameters.Add("?IO", MySqlDbType.Blob);
                    cmdUpdate1.Parameters.Add("?GSMInfo", MySqlDbType.UInt64);                    
                    
                    using (MySqlTransaction trans = db.connection.BeginTransaction())
                    {
                        while (true)
                            lock (m_Queue)
                                if (m_Queue.Count > 0)
                                {
                                    TrackerPacket point = m_Queue.Dequeue();                                    
                                    if (point.IsFixed(true) && !point.Write2DB(cmdUpdate))
                                        break;
                                    if (!point.Write2DB(cmdInsert))
                                        break;
                                    if (!point.Write2DB(cmdUpdate1))
                                        break;
                                }
                                else
                                    break;
                        trans.Commit();
                    }
                }
            }
            catch(Exception e)
            {
                m_Logger.Push(LogLevel.ERROR, 0, "Points " + e.ToString());
            }
            //online режим
            try
            {
                using (MySqlTransaction trans = db.connection.BeginTransaction())
                {
                    using (MySqlCommand cmd1 = new MySqlCommand("UPDATE curpos SET online = 0", db.connection))
                        cmd1.ExecuteNonQuery();

                    using (MySqlCommand cmd = new MySqlCommand("UPDATE curpos SET online = 1 WHERE TrackerID = ?ID", db.connection))
                    {
                        cmd.Parameters.Add("?ID", MySqlDbType.Int32);

                        lock (m_SessionTrackers)
                            foreach (KeyValuePair<uint, long> value in m_SessionTrackers)
                            {
                                cmd.Parameters["?ID"].Value = value.Value;
                                cmd.ExecuteNonQuery();
                            }
                    }
                    trans.Commit();
                }
            }
            catch (Exception e)
            {
                m_Logger.Push(LogLevel.ERROR, 0, "Events " + e.ToString());
            }
        }
        GC.Collect();
        m_bFlushing = false;
    }
}