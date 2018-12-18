using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Net.Sockets;

public class DingtekConnection: Connection
{
    public DingtekConnection(LogItemQueue logger, ServerCfg config)
        : base(logger, config)
    {
        m_Logger.Push(LogLevel.WARNING, 0, "Dingtek client started");
    }

    byte[] GetPacket(TrackerPacket point)
    {
        int i = 0;
        byte[] buf = new byte[45 + 20];
        for (int j = 0; j < buf.Length; j++)
            buf[j] = 0;
        
        buf[i++] = 0x29;
        buf[i++] = 0x29;
        buf[i++] = 0x80;
        i += 2; // length

        uint imei = (uint)(m_Tracker.m_IMEI - 14100000000);
        for (int k = 0; k < 4; k++)
        {
            buf[i + 3 - k] = (byte)(imei % 100);
            if (k != 2)
                buf[i + 3 - k] |= 0x80;
            imei = imei / 100;
        }
        i += 4;
        
        DateTime dt = CTime.GetTime(point.m_Time + 3600 * 2);
        buf[i++] = BCDConverter.GetBytes(dt.Year - 2000, 1)[0];
        buf[i++] = BCDConverter.GetBytes(dt.Month, 1)[0];
        buf[i++] = BCDConverter.GetBytes(dt.Day, 1)[0];
        buf[i++] = BCDConverter.GetBytes(dt.Hour, 1)[0];
        buf[i++] = BCDConverter.GetBytes(dt.Minute, 1)[0];
        buf[i++] = BCDConverter.GetBytes(dt.Second, 1)[0];

        GetCoord(point.m_fLat).CopyTo(buf, i); i += 4;
        GetCoord(point.m_fLng).CopyTo(buf, i); i += 4;

        BCDConverter.GetBytes(point.m_Speed, 2).CopyTo(buf, i); i += 2;
        BCDConverter.GetBytes(point.m_Direction, 2).CopyTo(buf, i); i += 2;

        decimal odo = 0, ignition = 0, oilcut = 0, pwr = 12000;
        decimal din1 = 0, din2 = 0, din3 = 0, din4 = 0;
        point.GetInput("DIN6", out ignition);
        point.GetInput("DIN7", out oilcut);
        point.GetInput("PWR", out pwr);
        point.GetInput("ODO", out odo);
        point.GetInput("DIN1", out din1);
        point.GetInput("DIN2", out din2);
        point.GetInput("DIN3", out din3);
        point.GetInput("DIN4", out din4);

        BIConverter.GetBytes((int)odo).CopyTo(buf, i);
        buf[i++] = (byte)(((pwr == 0) ? 16 : (pwr > 3000) ? 24 : 8) + ((point.IsFixed(false) ? 224 : 0)));
        i += 3;

        buf[i++] = (byte)(1 +
                           ((oilcut > 0) ? 0 : 4) +
                           ((din1 > 0) ? 0 : 8) +
                           ((din2 > 0) ? 0 : 16) +
                           ((din3 > 0) ? 0 : 32) +
                           ((din4 > 0) ? 0 : 64) +
                           ((ignition > 0) ? 0 : 128));//ign, pwr, oil cut
        buf[i++] = 0xFC;//alarms
        buf[i++] = 0x36;// 32;//tcp
        buf[i++] = 0x00;

        buf[i++] = 0x00;
        buf[i++] = 0x78;
        buf[i++] = 0x00;
        buf[i++] = 0x00;
        buf[i++] = 0x00;
        buf[i++] = 0x00;
        buf[i++] = 0x3C;
        buf[i++] = 0x00;

        decimal fuel = 0;
        if (point.GetInput("FUEL", out fuel))
        {
            string data = fuel.ToString("000.00").Replace(",", ".");
            BIConverter.GetBytes((ushort)(data.Length + 2)).CopyTo(buf, i); i += 2;
            BIConverter.GetBytes((ushort)0x0023).CopyTo(buf, i); i += 2;
            Encoding.ASCII.GetBytes(data).CopyTo(buf, i);
            i += data.Length;
        }
        if (point.GetInput("AIN5", out fuel))
        {
            BIConverter.GetBytes((ushort)4).CopyTo(buf, i); i += 2;
            BIConverter.GetBytes((ushort)0x002D).CopyTo(buf, i); i += 2;
            BIConverter.GetBytes((ushort)fuel).CopyTo(buf, i); i += 2;
        }

        BIConverter.GetBytes((ushort)(i - 3)).CopyTo(buf, 3); 
        buf[i++] = GetCRC(buf, 0, i);
        buf[i++] = 0x0D;

        byte[] res = new byte[i];

        string str = "<<";
        for (int j = 0; j < i; j++)
        {
            res[j] = buf[j];
            str += " " + buf[j].ToString("X2");
        }
        m_Logger.Push(LogLevel.INFO, 0, str);

        return res;
    }
    byte[] GetCoord(double f)
    {
        bool sign = f < 0;
        f = Math.Abs(f);
        int res = (int)Math.Truncate(f);
        res = res * 100000 + (int)Math.Round((f - res) * 60000);
        
        byte[] bytes = BCDConverter.GetBytes(res, 4);
        if (sign)
            bytes[0] |= 0x80;
        return bytes;
    }
    ////////////////////////////////////////////////////////////////////////
    protected override int DoSession(TrackerPacketList pt,int reSend)//TCP метод
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
                byte[] bytes = GetPacket(pt[i]);

                NetworkStream stream = client.GetStream();
                stream.Write(bytes, 0, bytes.Length);
                if (!client.Connected)
                    break;
                ret++;
            }
        client.Close();
       
        return ret;
    }
    ////////////////////////////////////////////////////////////////////////
    protected int DoSessionUPD(TrackerPacketList pt)//непонятно как будет работать в нескольких потоках
    {
        int ret = 0;

        UdpClient client = new UdpClient();
        client.Client.SendTimeout = m_config.Timeout;
        client.Client.ReceiveTimeout = m_config.Timeout;

        System.Net.IPEndPoint addr = null;
        string[] urlParts = m_config.URL.Split(':');
        int port = 0;
        if (urlParts.Length == 2 && int.TryParse(urlParts[1], out port))
        {
            addr = new System.Net.IPEndPoint(new System.Net.IPAddress(new byte[] { 173, 209, 57, 10 }), port);
            client.Connect(urlParts[0], port);
        }
        if (addr != null)
        {
            for (int i = 0; i < pt.Count; i++)
            {
                byte[] bytes = GetPacket(pt[i]);
                if (client.Send(bytes, bytes.Length) == bytes.Length)
                {
                    byte[] reply = client.Receive(ref addr);
                    if (reply.Length > 0)
                    {
                        string str = ">>";
                        for (int j = 0; j < reply.Length; j++)
                            str += " " + reply[j].ToString("X2");
                        m_Logger.Push(LogLevel.INFO, 0, str);

                        ret++;
                    }
                    else
                        break;
                }
            }
        }
        client.Close();

        return ret;
    }
    static public byte GetCRC(byte[] data, int from, int count)
    {
        int crc = 0;
        for (int i = 0; i < count; i++)
            crc = crc ^ data[i + from];
        return (byte)crc;
    }
}