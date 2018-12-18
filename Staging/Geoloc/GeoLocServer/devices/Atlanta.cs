using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
class AtlantaServer : AsyncServer
{
    public AtlantaServer(IPAddress ipAddres, int nPort) : base(ipAddres, nPort)
    {

    }
    ////////////////////////////////////////////////////////////////////////
    public override ServerSession CreateSession(Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
    {
        return new AtlantaSession(++m_nSessionCounter, socket, GPSQueue, logger);
    }
    //////////////////////////////////////////////////////////////////////////////////
    public override string ToString() { return "Atlanta"; }
    //////////////////////////////////////////////////////////////////////////////////
};
////////////////////////////////////////////////////////////////////////
//
////////////////////////////////////////////////////////////////////////
class AtlantaSession: ServerSession
{
    System.Globalization.NumberFormatInfo m_Format = new System.Globalization.NumberFormatInfo();
    ////////////////////////////////////////////////////////////////////////
    public AtlantaSession(uint nID, Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
        : base(nID, socket, GPSQueue, logger)
    {
        m_Format.NumberDecimalSeparator = ".";
    }
    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
    public override bool ProcessBuffer(byte[] buffer, int size)      //если вернет false - разорвать соединение
    {
        try
        {   
            string str = Encoding.ASCII.GetString(buffer, 0, size);
            LogEvent(LogLevel.DEBUG, "<< " + str);
          
            string[] packets = str.Replace("#$", "#\n$").Split(new Char[] { '\n', '\r' });
            for (int i = 0; i < packets.Length; i++)
                if (packets[i].Length > 3 && !ProcessPacket(packets[i]))
                    return false;

            return true;
        }
        catch (Exception e)
        {
            LogEvent(LogLevel.WARNING, "Ошибка обработки пакета " + e.ToString());
        }
        return false;
    }
    ////////////////////////////////////////////////////////////////////////
    ////ATL861693035354588,$GPRMC,061627,A,2306.4575,N,07238.3892,E,0,0,250317,,,*3D,#01111011000001,0,0,0,0.00,46.04,4.0,10,404,98,145d,d11cATL
    public bool ProcessPacket(string str)
    {
        if (str[0] != 0x20)
            return false;

        char msgType = str[1];

        string[] slices = str.Split(',');  //части сообщения

        if (m_IMEI == 0)
            if (!long.TryParse(slices[0].Substring(5, slices[0].Length - 5), out m_IMEI) || !IMEIIsValid())
                return false;

        TrackerPacket packet = new TrackerPacket(m_IMEI);
        if (slices[10].Length == 6 && slices[2].Length >= 6)
            packet.m_Time = CTime.GetTime(slices[10], slices[2]);

        packet.m_SatteliteCount = (slices[3] == "A") ? (byte)3 : (byte)0;

        if (slices[4].Length == 9)
            packet.m_fLat = (int.Parse(slices[4].Substring(0, 2)) + (float.Parse(slices[4].Substring(2, 7), m_Format) / 60)) * (slices[5] == "S" ? -1 : 1);
        if (slices[6].Length == 10)
            packet.m_fLng = (int.Parse(slices[6].Substring(0, 3)) + (float.Parse(slices[6].Substring(3, 7), m_Format) / 60)) * (slices[7] == "W" ? -1 : 1);
        if (slices[8].Length > 0)
            packet.m_Speed = (ushort)(float.Parse(slices[8], m_Format) * 1.852);
        if (slices[9].Length > 0)
            packet.m_Direction = (ushort)(float.Parse(slices[9], m_Format));

        slices[14] = slices[14].Replace("#", "");
        packet.SetInput("DIN1", (slices[14][0] == '1') ? 1 : 0);
        packet.SetInput("DIN2", (slices[14][1] == '1') ? 1 : 0);
        packet.SetInput("ALARM", (slices[14][2] == '0') ? 1 : 0);
        packet.SetInput("ACCEL", (slices[14][8] == '1') ? 1 : 0);
        packet.SetInput("BREAK_ACCEL", (slices[14][9] == '1') ? 1 : 0);

        packet.SetInput("PWR", float.Parse(slices[15], m_Format) * 12);
        packet.SetInput("ODO", float.Parse(slices[18], m_Format));
        packet.SetInput("ACC", float.Parse(slices[20], m_Format));

        return PushPacket(packet);
    }
};