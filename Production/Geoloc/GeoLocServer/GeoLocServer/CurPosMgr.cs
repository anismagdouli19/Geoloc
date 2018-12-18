using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;

class TrackerPacketManager
{
    protected LogItemQueue m_Logger;

    Timer m_Timer = null;
    string m_strTable = "";

    protected Dictionary<long, TrackerPacket> m_Points = new Dictionary<long, TrackerPacket>();
    protected Dictionary<long, TrackerPacket> m_Points4DB = new Dictionary<long, TrackerPacket>();

    bool m_bRunning = false;
    //////////////////////////////////////////////////////////////////////////
    public TrackerPacketManager(LogItemQueue logger, string strTable)
    {
        m_Logger = logger;
        m_Timer = new Timer(Tick, null, 0, 2000);
        m_strTable = strTable;
        Load();
    }
    ~TrackerPacketManager()
    {
        if (m_Timer != null)
            m_Timer.Dispose();
    }

    public TrackerPacketList GetPackets()
    {
        TrackerPacketList points = new TrackerPacketList();
        lock (m_Points)
            foreach (TrackerPacket packet in m_Points.Values)
                points.Add(packet);
        return points;
    }
    public TrackerPacket GetPacket(int TrackerID)
    {
        lock (m_Points)
            if (m_Points.ContainsKey(TrackerID))
                return m_Points[TrackerID];
        return null;
    }

    //////////////////////////////////////////////////////////////////////////
    void Tick(object obj)
    {
        if (m_bRunning)
            return;
        m_bRunning = true;
        try
        {
            TrackerPacketList list = new TrackerPacketList();
            lock (m_Points)
            {
                foreach (TrackerPacket packet in m_Points4DB.Values)
                    list.Add(packet);
                m_Points4DB.Clear();
            }

            if (list.Count > 0)
            using (CDatabase db = Configuration.inst().GetDB())
            {
                if (db.IsConnected())
                {
                    using (MySqlCommand cmdUpdate = new MySqlCommand("UPDATE " + m_strTable + " SET Time=?Time, Lat=?Lat, Lng=?Lng, Status=?Status, Speed=?Speed, Alt=?Alt, IO=?IO, GSMInfo=?GSMInfo WHERE TrackerID = ?ID AND Time <= ?Time", db.connection))
                    {
                        cmdUpdate.Parameters.Add("?ID", MySqlDbType.Int32);
                        cmdUpdate.Parameters.Add("?Time", MySqlDbType.Int32);
                        cmdUpdate.Parameters.Add("?Lat", MySqlDbType.Float);
                        cmdUpdate.Parameters.Add("?Lng", MySqlDbType.Float);
                        cmdUpdate.Parameters.Add("?Alt", MySqlDbType.Int32);
                        cmdUpdate.Parameters.Add("?Speed", MySqlDbType.Int32);
                        cmdUpdate.Parameters.Add("?Status", MySqlDbType.Int32);
                        cmdUpdate.Parameters.Add("?IO", MySqlDbType.Blob);
                        cmdUpdate.Parameters.Add("?GSMInfo", MySqlDbType.UInt64);
                        using (MySqlTransaction trans = db.connection.BeginTransaction())
                        {
                            for (int i = 0; i < list.Count; i++)
                                if (!list[i].Write2DB(cmdUpdate))
                                    break;
                            trans.Commit();
                        }
                    }
                }
                else
                    m_Logger.Push(LogLevel.ERROR, 0, "Could not connect to database");
            }
        }
        catch (Exception e)
        {
            m_Logger.Push(LogLevel.ERROR, 0, e.ToString());
        }
        m_bRunning = false;
    }
    void Load()
    {
        List<TrackerPacket> newPackets = new List<TrackerPacket>();
        try
        {
            using (CDatabase db = Configuration.inst().GetDB())
            {
                if (!db.IsConnected())
                {
                    m_Logger.Push(LogLevel.ERROR, 0, "Could not connect to database");
                    return;
                }

                //загрузка точек
                TrackerPacketList list = new TrackerPacketList();
                list.Load(db, "SELECT TrackerID, Time, Lat, Lng, Alt, Speed, Status, IO, GSMInfo FROM " + m_strTable);

                for (int i = 0; i < list.Count; i++ )
                {
                    TrackerPacket packet = list[i];
                    if (packet.IsValid())
                        lock (m_Points)
                            if (!m_Points.ContainsKey(packet.m_ID) || m_Points[packet.m_ID].m_Time != packet.m_Time)
                            {
                                m_Points[packet.m_ID] = packet;
                                newPackets.Add(packet);
                            }
                }
            }

            for (int i = 0; i < newPackets.Count; i++)
                OnCurPosChange(newPackets[i]);        
        }
        catch (Exception e)
        {
            m_Logger.Push(LogLevel.ERROR, 0, e.ToString());
        }
    }
    protected virtual void OnCurPosChange(TrackerPacket packet){}

    public void AddPoint(TrackerPacket point)
    {
        lock (m_Points)
        {
            TrackerPacket packet = null;
            if (!m_Points.TryGetValue(point.m_ID, out packet) || point.m_Time > packet.m_Time)
            {
                m_Points[point.m_ID] = point;
                OnCurPosChange(point);
                m_Points4DB[point.m_ID] = point;
            }
        }
    }
}

class LastPointMgr : TrackerPacketManager
{
    LastPointMgr(LogItemQueue logger): base(logger, "lastpoint")
    {
        m_Logger.Push(LogLevel.WARNING, 0, "LastPointMgr started");
    }

    private static LastPointMgr _inst = null;
    public static LastPointMgr inst()
    {
        if (_inst == null)
            _inst = new LastPointMgr(Global.m_Logger.GetQueue("LastPoint"));
        return _inst;
    }

    protected override void OnCurPosChange(TrackerPacket packet)
    {
        CTracker tracker = ConfigMgr.inst().GetTrackerByID((int)packet.m_ID);
        if (tracker == null)
            return;
    }
}

class CurPosMgr : TrackerPacketManager
{
    //Timer tmptimer = null;
    public CurPosMgr(LogItemQueue logger): base(logger, "curpos")
    {
        m_Logger.Push(LogLevel.WARNING, 0, "CurPosMgr started");
        //tmptimer = new Timer(TmpTick, null, 1000, 5000);
    }

    private static CurPosMgr _inst = null;
    public static CurPosMgr inst()
    {
        if (_inst == null)
            _inst = new CurPosMgr(Global.m_Logger.GetQueue("CurPos"));
        return _inst;
    }
    protected override void OnCurPosChange(TrackerPacket packet)
    {
        CTracker tracker = ConfigMgr.inst().GetTrackerByID((int)packet.m_ID);
        if (tracker == null)
            return;

    }
}