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
class VisiontekServer : AsyncServer
{
    public VisiontekServer(IPAddress ipAddres, int nPort): base(ipAddres, nPort)
    {

    }
    ////////////////////////////////////////////////////////////////////////
    public override ServerSession CreateSession(Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
    {
        return new VisiontekSession(++m_nSessionCounter, socket, GPSQueue, logger);
    }
    //////////////////////////////////////////////////////////////////////////////////
    public override string ToString() { return "Visiontek"; }
    //////////////////////////////////////////////////////////////////////////////////
};
////////////////////////////////////////////////////////////////////////
//
////////////////////////////////////////////////////////////////////////
class VisiontekSession: ServerSession
{
    System.Globalization.NumberFormatInfo m_Format = new System.Globalization.NumberFormatInfo();
    ////////////////////////////////////////////////////////////////////////
    public VisiontekSession(uint nID, Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
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
            //str = "$2,VISIONTEK,356308049407395,22,03,17,13,23,11,23.047885N,072.526990E,000.0,149,09.4,0338,0,0,0,0,0,0,0,0,0,00.00,00.00,00,00,0000,00.0,3.8,18,06,0000000000000,A,S#$1,VISIONTEK,356308049407395,22,03,17,13,23,50,00.0000000,000.0000000,000.0,000,00.0,0000,0,0,0,0,0,0,0,0,0,00.01,00.00,00,00,0000,00.0,3.9,18,00,0000000000000,V,M#$1,VISIONTEK,356308049407395,22,03,17,13,23,59,00.0000000,000.0000000,000.0,000,00.0,0000,0,0,0,0,0,0,0,0,0,00.00,00.00,00,00,0000,00.0,3.9,18,00,0000000000000,V,M#$1,VISIONTEK,356308049407395,22,03,17,13,24,09,00.0000000,000.0000000,000.0,000,00.0,0000,0,0,0,0,0,0,0,0,0,00.00,00.00,00,00,0000,00.0,3.9,18,00,0000000000000,V,S#$1,VISIONTEK,356308049407395,22,03,17,13,24,19,00.0000000,000.0000000,000.0,000,00.0,0000,0,0,0,0,0,0,0,0,0,00.00,00.00,00,00,0000,00.0,3.9,18,00,0000000000000,V,M#$1,VISIONTEK,356308049407395,22,03,17,13,24,29,00.0000000,000.0000000,000.0,000,00.0,0000,0,0,0,0,0,0,0,0,0,00.00,00.00,00,00,0000,00.0,3.9,17,00,0000000000000,V,M#$1,VISIONTEK,356308049407395,22,03,17,13,24,39,00.0000000,000.0000000,000.0,000,00.0,0000,0,0,0,0,0,0,0,0,0,00.00,00.00,00,00,0000,00.0,3.9,17,00,0000000000000,V,M#";
            LogEvent(LogLevel.DEBUG, "<< " + str);
          
            string[] packets = str.Replace("#$", "#\n$").Split(new Char[] { '\n', '\r' });
            for (int i = 0; i < packets.Length; i++)
            {
                if (packets[i].Length < 3)
                    continue;
                if (!ProcessPacket(packets[i]))
                    return false;
            }
            return true;
        }
        catch (Exception e)
        {
            LogEvent(LogLevel.WARNING, "Ошибка обработки пакета " + e.ToString());
        }
        return false;
    }
    ////////////////////////////////////////////////////////////////////////
    public bool ProcessPacket(string str)
    {
        if (str[0] != '$' || str[str.Length - 1] != '#')
            return false;

        string[] slices = str.Substring(1, str.Length - 2).Split(',');  //части сообщения

        if (m_IMEI == 0)
            if (!long.TryParse(slices[2], out m_IMEI) || !IMEIIsValid())
                return false;

        switch (slices[0])
        {
            //case "0": //power on
                //return ParsePositionPacket(slices);//после числа спутников другой порядок
            case "1": //position
            case "2": //towing
                return ParsePositionPacket(slices);
            default:
                return true;
        }

        return false;
    }
    ////////////////////////////////////////////////////////////////////////
    bool ParsePositionPacket(string[] slices)
    {
        TrackerPacket packet = new TrackerPacket(m_IMEI);
        packet.m_Time = CTime.GetTime(slices[3] + slices[4] + slices[5], slices[6] + slices[7] + slices[8]);

        //координаты
        packet.m_fLat = float.Parse(slices[9].Substring(0, slices[9].Length - 1), m_Format) * (slices[9][slices[9].Length - 1] == 'S' ? -1 : 1);
        packet.m_fLng = float.Parse(slices[10].Substring(0, slices[10].Length - 1), m_Format) * (slices[10][slices[10].Length - 1] == 'W' ? -1 : 1);

        packet.m_Speed = (ushort)float.Parse(slices[11], m_Format);
        packet.m_Direction = ushort.Parse(slices[12]);
        packet.SetInput("HDOP", float.Parse(slices[13], m_Format));
        packet.m_Alt = (short)float.Parse(slices[14], m_Format);
        packet.SetInput("ODO", float.Parse(slices[15], m_Format));

        packet.SetInput("DIN1", (slices[16] == "1") ? 1 : 0);
        packet.SetInput("DIN2", (slices[17] == "1") ? 1 : 0);
        packet.SetInput("DIN3", (slices[18] == "1") ? 1 : 0);
        packet.SetInput("DIN4", (slices[19] == "1") ? 1 : 0);
        packet.SetInput("DOUT1", (slices[20] == "1") ? 1 : 0);
        packet.SetInput("DOUT2", (slices[21] == "1") ? 1 : 0);
        packet.SetInput("DOUT3", (slices[22] == "1") ? 1 : 0);
        packet.SetInput("DOUT4", (slices[23] == "1") ? 1 : 0);

        packet.SetInput("AIN1", float.Parse(slices[24], m_Format));
        packet.SetInput("AIN2", float.Parse(slices[25], m_Format));
        packet.SetInput("AIN3", float.Parse(slices[26], m_Format));
        packet.SetInput("AIN4", float.Parse(slices[27], m_Format));

        //iWare  28

        packet.SetInput("PWR", float.Parse(slices[29], m_Format));
        packet.SetInput("ACC", float.Parse(slices[30], m_Format));
        //packet.SetInput("GSM", float.Parse(slices[31], m_Format));
        packet.m_SatteliteCount = (slices[34] == "A") ? byte.Parse(slices[32]) : (byte)0;
        //packet.SetInput("RS232", long.Parse(slices[33]));

        return PushPacket(packet,m_IMEI);
  
    }
};