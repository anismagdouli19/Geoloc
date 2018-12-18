using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Net.Sockets;

public class DIMTSConnection : Connection
{
    public DIMTSConnection(LogItemQueue logger, ServerCfg config)
        : base(logger, config)
    {
        m_Logger.Push(LogLevel.WARNING, 0, "DIMTS client started");
    }

    // У$DIMTS,null,null,null,357852031069696,DL1PC4716,31.12.2013,06:54:11,28.5271550,null,77.2807010,null,12.5,1,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null#Ф
    string GetPacket(TrackerPacket point)
    {
        DateTime dt = CTime.GetTime(point.m_Time);
        decimal odo = 0, pwr = 0, acc = 0;
        point.GetInput("ODO", out odo);
        point.GetInput("ACC", out acc);
        point.GetInput("PWR", out pwr);

        string res = "$DIMTS,null,null,null";
        res += "," + m_Tracker.m_IMEI;
        res += "," + m_Tracker.m_strName;
        res += "," + dt.ToString("dd.MM.yyyy");
        res += "," + dt.ToString("HH:mm:ss");
        res += "," + CObject.Float2XML(Math.Abs(point.m_fLat));
        res += "," + ((point.m_fLat >= 0) ? "N" : "S");
        res += "," + CObject.Float2XML(Math.Abs(point.m_fLng));
        res += "," + ((point.m_fLng >= 0) ? "E" : "W");
        res += "," + point.m_Speed;
        res += "," + (point.IsFixed(false) ? "1" : "0");
        res += "," + point.m_SatteliteCount;
        res += ",null";// + 1; //DOP
        res += "," + point.m_Direction;
        res += "," + point.m_Alt;
        res += ",null";// + 0;
        res += "," + Math.Round(odo); //ODO
        res += ",null"; //IGN
        res += ",null";// + CObject.Float2XML((double)acc / 1000.0); //Batt
        res += ",null";// + CObject.Float2XML((double)pwr / 1000.0); //POWER
        res += ",null";// + "00000000"; //Digitalinputvalues
        res += ",null"; //NetworkOperatorName
        res += ",null";// + 15; //GSMStrength
        res += ",null";// + 0; //CellID
        res += ",null";// + 0; //LAC
        res += ",null";// + 1; //SeqNo
        res += ",null";// + 0; //AvgSpd
        res += ",null";// + 0; //MinSpd
        res += "#";

        m_Logger.Push(LogLevel.INFO, 0, res);

        return res;
    }
    ////////////////////////////////////////////////////////////////////////
    protected override int DoSession(TrackerPacketList pt,int isResend)//TCP метод
    {
        int ret = 0;

        TcpClient client = new TcpClient();
        client.SendTimeout = m_config.Timeout;
        client.ReceiveTimeout = m_config.Timeout;

        string[] urlParts = m_config.URL.Split(':'); 
        int port = 0;
        if (urlParts.Length == 2 && int.TryParse(urlParts[1], out port))
            client.Connect(urlParts[0], port);//urlParts[0], port
        if (client.Connected)
            for (int i = 0; i < pt.Count; i++)
            {
                string packet = GetPacket(pt[i]);
                byte[] bytes = Encoding.ASCII.GetBytes(packet);
                NetworkStream stream = client.GetStream();
                stream.Write(bytes, 0, bytes.Length);
                if (!client.Connected)
                    break;
                ret++;
            }
        client.Close();
       
        return ret;
    }
}