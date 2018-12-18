using System;
using System.Text;
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
        m_FlushDataTimer = new Timer(new TimerCallback(FlushData), null, 20000, 5000);

        m_Logger.Push(LogLevel.WARNING, 0, "GPSPointQueue started");
    }
    //////////////////////////////////////////////////////////////////////
    ~GPSPointQueue()
    {
        if (m_FlushDataTimer != null)
            m_FlushDataTimer.Dispose();
    }
    private static GPSPointQueue _inst = null;
    public static GPSPointQueue inst()
    {
        if (_inst == null)
            _inst = new GPSPointQueue(Global.m_Logger.GetQueue("GPSQueue"));
        return _inst;
    }
    //////////////////////////////////////////////////////////////////////
    public bool PushPacket(TrackerPacket packet)
    {
        CTracker tracker = (packet.m_ID > 0) ? ConfigMgr.inst().GetTrackerByIMEI(packet.m_ID) : null;
        if (tracker != null)
        {
            packet.m_ID = tracker.m_nID;
            return PushPacketWithID(packet);
        }
        return false;
    }
    //////////////////////////////////////////////////////////////////////
    public bool PushPacketWithID(TrackerPacket packet)
    {
        try
        {
            CTracker tracker = ConfigMgr.inst().GetTrackerByID((int)packet.m_ID);
            if (tracker != null)
            {

                if (packet.IsValid())
                {
                    lock (m_Queue)
                        m_Queue.Enqueue(packet);

                    if (packet.IsFixed(true))
                        CurPosMgr.inst().AddPoint(packet);
                    LastPointMgr.inst().AddPoint(packet);
                }
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
        CTracker tracker = (IMEI > 0) ? ConfigMgr.inst().GetTrackerByIMEI(IMEI) : null;
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
            if (m_Queue.Count > 300000)
            {
                while (m_Queue.Count > 300000)
                    m_Queue.Dequeue();
                m_Logger.Push(LogLevel.ERROR, 0, "points was purged from queue");
            }

        using (CDatabase db = Configuration.inst().GetDB())
        {

            if (!db.IsConnected())
            {
                m_bFlushing = false;
                m_Logger.Push(LogLevel.ERROR, 0, "Не удалось соединиться с БД");
                return;
            }

            DateTime dtFrom = DateTime.Now;
            int nPointCount = 0;

            if (m_Queue.Count > 0)
            try
            {
                using (MySqlCommand cmdInsert = new MySqlCommand("INSERT IGNORE INTO points(TrackerID, Time, Lat, Lng, Status, Speed, Alt, IO, GSMInfo,ForwardTime,ReceivedTime) VALUES(?ID, ?Time, ?Lat, ?Lng, ?Status, ?Speed, ?Alt, ?IO, ?GSMInfo,?ForwardTime,?ReceivedTime)", db.connection))
                {
                    cmdInsert.Parameters.Add("?ID", MySqlDbType.Int32);
                    cmdInsert.Parameters.Add("?Time", MySqlDbType.Int32);
                    cmdInsert.Parameters.Add("?Lat", MySqlDbType.Float);
                    cmdInsert.Parameters.Add("?Lng", MySqlDbType.Float);
                    cmdInsert.Parameters.Add("?Alt", MySqlDbType.Int32);
                    cmdInsert.Parameters.Add("?Speed", MySqlDbType.Int32);
                    cmdInsert.Parameters.Add("?Status", MySqlDbType.Int32);
                    cmdInsert.Parameters.Add("?IO", MySqlDbType.Blob);
                    cmdInsert.Parameters.Add("?GSMInfo", MySqlDbType.UInt64);
                    cmdInsert.Parameters.Add("?ForwardTime",MySqlDbType.Int32);
                    cmdInsert.Parameters.Add("?ReceivedTime", MySqlDbType.Int32);
                    using (MySqlTransaction trans = db.connection.BeginTransaction())
                    {
                        while (++nPointCount < 5000 && m_Queue.Count > 0)
                        {
                            TrackerPacket point = null;
                            lock (m_Queue)
                                point = m_Queue.Dequeue();                            
                            if (point == null || !point.Write2DB(cmdInsert))
                                break;                            
                        }
                        trans.Commit();
                    }
                }
            }
            catch(Exception e)
            {
                m_Logger.Push(LogLevel.ERROR, 0, "Points " + e.ToString());
            }

            DateTime dtPoints = DateTime.Now;

            DateTime dtFuel = DateTime.Now;

            DateTime dtPhoto = DateTime.Now;

            DateTime dtEkey = DateTime.Now;
            //online режим
            try
            {
                StringBuilder str = new StringBuilder("update curpos set online = (trackerid in (0");
                lock (m_SessionTrackers)
                    foreach (KeyValuePair<uint, long> value in m_SessionTrackers)
                        str.Append("," + value.Value);
                str.Append("))");
                
                using (MySqlCommand cmd = new MySqlCommand(str.ToString(), db.connection))
                    cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                m_Logger.Push(LogLevel.ERROR, 0, "Curpos " + e.ToString());
            }
            double t = ((TimeSpan)(DateTime.Now - dtFrom)).TotalMilliseconds;
            double t1 = ((TimeSpan)(dtPoints - dtFrom)).TotalMilliseconds;
            double t2 = ((TimeSpan)(dtFuel - dtPoints)).TotalMilliseconds;
            double t3 = ((TimeSpan)(dtPhoto - dtFuel)).TotalMilliseconds;
            double t4 = ((TimeSpan)(dtEkey - dtPhoto)).TotalMilliseconds;
            double t5 = ((TimeSpan)(DateTime.Now - dtEkey)).TotalMilliseconds;
            if (t > 300)
                m_Logger.Push(LogLevel.WARNING, 0, "GPSQueue: " + nPointCount + " pts. Times:" + t1 + "/" + t2 + "/" + t3 + "/" + t4 + "/" + t5 + "/");
        }
        GC.Collect();
        m_bFlushing = false;
    }
}
