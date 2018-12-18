using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Net.Sockets;

public class WialonIPSConnection: Connection
{
    public WialonIPSConnection(LogItemQueue logger, ServerCfg config)
        : base(logger, config)
    {
        m_Logger.Push(LogLevel.WARNING, 0, "Wialon client started");
    }

    string GetCoord(float f, bool lat)
    {
        string s = Math.Truncate(f).ToString(lat ? "00" : "000");
        f = Math.Abs(f - (float)Math.Truncate(f));
        f *= 60;
        s += f.ToString("00.0000").Replace(",",".");
        return s;
    }

    string GetPacket(TrackerPacket point)
    {
        decimal key = 0;
        point.GetInput("EKEY", out key);

        DateTime dt = CTime.GetTime(point.m_Time);
        string str = dt.ToString("ddMMyy") + ";" +
                     dt.ToString("HHmmss") + ";" +
                     GetCoord(Math.Abs(point.m_fLat), true) + ";" +
                     ((point.m_fLat > 0) ? "N" : "S") + ";" +
                     GetCoord(Math.Abs(point.m_fLng), false) + ";" +
                     ((point.m_fLng > 0) ? "E" : "W") + ";" +
                     point.m_Speed + ";" +
                     point.m_Direction + ";" +
                     point.m_Alt + ";" +
                     point.m_SatteliteCount + ";" +
                     "0" + ";" +
                     ";" +
                     ";" +
                     ";" +
                     ((key > 0) ? ((long)key).ToString("X12") : "NA") + ";";
        byte[] inputs = point.GetIOBuffer();
        if (inputs != null)
        {
            int i = 0;
            while (i < inputs.Length)
            {
                ushort id = BitConverter.ToUInt16(inputs, i);
                bool b64bit = (id & (1 << (int)TrackerPacket.IOFlags.LENGTH8)) != 0;
                i += 2 + (b64bit ? 8 : 4);

                decimal fValue = 0;
                string name = IOChannelMgr.inst().GetIOChannel((ushort)(id & 0xFFF));
                if (name != null && point.GetInput(name, out fValue))
                {
                    if ((id & (1 << (int)TrackerPacket.IOFlags.INTEGER)) != 0)
                        str += name + ":1:" + fValue + ",";
                    else
                        str += name + ":2:" + CObject.Float2XML(fValue) + ",";
                }
            }
        }
        return str.Trim(',');
    }
    ////////////////////////////////////////////////////////////////////////
    protected override int DoSession(TrackerPacketList pt,int reSend)
    {
        int ret = 0;

        TcpClient client = new TcpClient();
        client.ReceiveTimeout = m_config.Timeout;
        client.SendTimeout = 1000;

        string[] urlParts = m_config.URL.Split(':'); 
        int port = 0;
        if (urlParts.Length == 2 && int.TryParse(urlParts[1], out port))
            client.Connect(urlParts[0], port);
        if (client.Connected)   //m_config.URL
        {
            NetworkStream stream = client.GetStream();

            byte[] ByteQuery = Encoding.UTF8.GetBytes("#L#" + m_Tracker.m_IMEI + ";NA\r\n");

            stream.Write(ByteQuery, 0, ByteQuery.Length);
            m_Logger.Push(LogLevel.INFO, 0, ">> " + "#L#" + m_Tracker.m_IMEI + ";NA\r\n");

            byte[] reply = new byte[7];
            if (stream.Read(reply, 0, reply.Length) == reply.Length && 
                Encoding.ASCII.GetString(reply, 0, reply.Length) == "#AL#1\r\n")
            {
                StringBuilder str = new StringBuilder("#B#");
                for (int i = 0; i < pt.Count; i++)
                {
                    if (i > 0)
                        str.Append("|");
                    str.Append(GetPacket(pt[i]));
                }
                str.Append("\r\n");
                m_Logger.Push(LogLevel.INFO, 0, ">> " + str);
                ByteQuery = Encoding.UTF8.GetBytes(str.ToString());
                stream.Write(ByteQuery, 0, ByteQuery.Length);

                if (pt.Count > 9)
                    reply = new byte[(pt.Count > 99) ? 9 : 8];

                if (stream.Read(reply, 0, reply.Length) == reply.Length &&
                    Encoding.ASCII.GetString(reply, 0, reply.Length).Replace("#AB#", "").Replace("\r\n", "") == pt.Count.ToString())
                    ret = pt.Count;
            }
        }

        client.Close();
       
        return ret;
    }
}