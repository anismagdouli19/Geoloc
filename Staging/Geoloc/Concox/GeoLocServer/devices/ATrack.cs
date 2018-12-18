using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
class ATrackServer : AsyncServer
{
    public ATrackServer(IPAddress ipAddres, int nPort): base(ipAddres, nPort)
    {

    }
    ////////////////////////////////////////////////////////////////////////
    public override ServerSession CreateSession(Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
    {
        return new ATrackSession(++m_nSessionCounter, socket, GPSQueue, logger);
    }
    //////////////////////////////////////////////////////////////////////////////////
    public override string ToString() { return "ATrack"; }
    //////////////////////////////////////////////////////////////////////////////////
};
////////////////////////////////////////////////////////////////////////
//
////////////////////////////////////////////////////////////////////////
class ATrackSession : ServerSession
{
    ushort m_SeqID = 0;
    ////////////////////////////////////////////////////////////////////////
    public ATrackSession(uint nID, Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger): base(nID, socket, GPSQueue, logger)
    {

    }
    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
    public override bool ProcessBuffer(byte[] buffer, int size)      //если вернет false - разорвать соединение
    {
        string str = Encoding.ASCII.GetString(buffer, 0, size);
        LogEvent(LogLevel.DEBUG, "<< " + str);

        if (buffer[0] != 0xFE)
        {
            string[] packets = str.Split('\n');
            for (int i = 0; i < packets.Length; i++)
                if (packets[i].Length > 2 && !ProcessPacket(packets[i]))
                    return false;
        }
        else
        {
            int from = 0;
            while (from < size)
                if (!ProcessBinaryPacket(buffer, ref from))
                    return false;
        }
        byte[] reply = new byte[12] { 0xFE, 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        BIConverter.GetBytes(m_IMEI).CopyTo(reply, 2);
        BIConverter.GetBytes(m_SeqID).CopyTo(reply, 10);
        Send(reply, reply.Length);
        return true;
    }
    bool ProcessPacket(string str)
    {
        int offset = (str[0] == '@') ? 0 : 5;
        //@P,D148,189,4158,862877031795188,1504877077,1504877078,1504877078,72974274,21104756,266,2,113962,8,0,0,0,0,,251,2000,,%CI%SA%MV%BV%GQ%AT%RP%CE%LC%CN%RL%GS%DT,9,127,41,16,36,0,21646,5189,40498,0,9,0
        string[] parts = str.Split(',');
        if (parts.Length <= 20 - offset)
            return false;

        if (offset == 0)
        {
            if (m_IMEI == 0)
            {
                m_IMEI = long.Parse(parts[4]);
                if (!IMEIIsValid())
                    return false;
            }
            m_SeqID = ushort.Parse(parts[3]);
        }

        TrackerPacket packet = new TrackerPacket(m_IMEI);
        packet.m_Time = int.Parse(parts[5 - offset]);
        packet.m_fLng = (float)(int.Parse(parts[8 - offset]) * 0.000001);
        packet.m_fLat = (float)(int.Parse(parts[9 - offset]) * 0.000001);
        packet.m_Direction = ushort.Parse(parts[10 - offset]);
        packet.SetInput("ODO", int.Parse(parts[12 - offset]) * 0.1);
        packet.SetInput("HDOP", int.Parse(parts[13 - offset]) * 0.1);

        string io = parts[14 - offset];
        for (int i = 0; i < io.Length; i++)
            packet.SetInput("DIN" + i, io[io.Length - i - 1] != '0');

        packet.m_Speed = ushort.Parse(parts[15 - offset]);

        io = parts[16 - offset];
        for (int i = 0; i < io.Length; i++)
            packet.SetInput("DOUT" + i, io[io.Length - i - 1] != '0');

        packet.SetInput("AIN1", int.Parse(parts[17 - offset]) * 0.001);
        if (parts[19 - offset] != "2000")
            packet.SetInput("TEMP1", int.Parse(parts[19 - offset]) * 0.1);
        if (parts[20 - offset] != "2000")
            packet.SetInput("TEMP2", int.Parse(parts[20 - offset]) * 0.1);

        packet.m_SatteliteCount = 3;

        return PushPacket(packet,m_IMEI);
    }
    bool ProcessBinaryPacket(byte[] buffer, ref int from)
    {
        if (m_IMEI == 0)
        {
            m_IMEI = BIConverter.ToInt64(buffer, from + 2);
            if (!IMEIIsValid())
                return false;
        }
        m_SeqID = BIConverter.ToUInt16(buffer, from + 10);
        from += 12;
        return true;
    }
    //////////////////////////////////////////////////////////////////////    
}


////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
class ATrackUDPServer : AsyncUDPServer
{   
    
    public ATrackUDPServer(IPAddress ipAddres, int nPort): base(ipAddres, nPort)
    {
    }
    //////////////////////////////////////////////////////////////////////////////////
    public override string ToString() { return "ATrack.UDP"; }
    //////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
    public override bool ProcessBuffer(byte[] buffer, int size)      //если вернет false - разорвать соединение
    {
        string str = Encoding.ASCII.GetString(buffer, 0, size);
        LogEvent(LogLevel.DEBUG, "<< " + str);

        long IMEI = 0;
        string[] packets = str.Split('\n');
        for (int i = 0; i < packets.Length; i++)
            if (packets[i].Length > 2 && !ProcessPacket(packets[i], ref IMEI))
                return false;
        return true;
    }
    ////////////////////////////////////////////////////////////////////////
    public bool ProcessPacket(string str, ref long m_IMEI)
    {
        int offset = (str[0] == '@') ? 0 : 5;
        //@P,D148,189,4158,862877031795188,1504877077,1504877078,1504877078,72974274,21104756,266,2,113962,8,0,0,0,0,,251,2000,,%CI%SA%MV%BV%GQ%AT%RP%CE%LC%CN%RL%GS%DT,9,127,41,16,36,0,21646,5189,40498,0,9,0
        string[] parts = str.Split(',');
        if (parts.Length <= 20 - offset)
            return false;

        if (offset == 0)
            if (m_IMEI == 0)
                m_IMEI = long.Parse(parts[4]);

        TrackerPacket packet = new TrackerPacket(m_IMEI);
        packet.m_Time = int.Parse(parts[5 - offset]);
        packet.m_fLng = (float)(int.Parse(parts[8 - offset]) * 0.000001);
        packet.m_fLat = (float)(int.Parse(parts[9 - offset]) * 0.000001);
        packet.m_Direction = ushort.Parse(parts[10 - offset]);
        packet.SetInput("ODO", int.Parse(parts[12 - offset]) * 0.1);
        packet.SetInput("HDOP", int.Parse(parts[13 - offset]) * 0.1);

        string io = parts[14 - offset];
        for (int i = 0; i < io.Length; i++)
            packet.SetInput("DIN" + i, io[io.Length - i - 1] != '0');

        packet.m_Speed = ushort.Parse(parts[15 - offset]);

        io = parts[16 - offset];
        for (int i = 0; i < io.Length; i++)
            packet.SetInput("DOUT" + i, io[io.Length - i - 1] != '0');

        packet.SetInput("AIN1", int.Parse(parts[17 - offset]) * 0.001);
        if (parts[19 - offset] != "2000")
            packet.SetInput("TEMP1", int.Parse(parts[19 - offset]) * 0.1);
        if (parts[20 - offset] != "2000")
            packet.SetInput("TEMP2", int.Parse(parts[20 - offset]) * 0.1);

        packet.m_SatteliteCount = 3;

        return PushPacket(packet,m_IMEI);
    }
}