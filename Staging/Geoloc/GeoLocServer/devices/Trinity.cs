using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;

namespace GeoLoc.devices
{
    class TrinityServer : AsyncServer
    {
        public TrinityServer(IPAddress ipAddres, int nPort):base(ipAddres, nPort)
        {
        }
        ////////////////////////////////////////////////////////////////////////
        public override ServerSession CreateSession(Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
        {
            return new TrinitySession(++m_nSessionCounter, socket, GPSQueue, logger);
        }
        //////////////////////////////////////////////////////////////////////////////////
        public override string ToString() { return "Trinity"; }
        //////////////////////////////////////////////////////////////////////////////////
    };
    class TrinitySession : ServerSession
    {
        System.Globalization.NumberFormatInfo m_Format = new System.Globalization.NumberFormatInfo();
        ////////////////////////////////////////////////////////////////////////
        public TrinitySession(uint nID, Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger):base(nID, socket, GPSQueue, logger)
        {
            m_Format.NumberDecimalSeparator = ".";
        }
        ////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////
        public override bool ProcessBuffer(byte[] buffer, int size)      //åñëè âåðíåò false - ðàçîðâàòü ñîåäèíåíèå
        {
            try
            {
                string str = Encoding.ASCII.GetString(buffer, 0, size);
                LogEvent(LogLevel.DEBUG, "<< " + str);

                string[] packets = str.Replace("$", "#\n$").Split(new Char[] { '\n', '\r' });
                for (int i = 0; i < packets.Length; i++)
                    if (packets[i].Length > 3 && !ProcessPacket(packets[i]))
                        return false;

                return true;
            }
            catch (Exception e)
            {
                LogEvent(LogLevel.WARNING, "Îøèáêà îáðàáîòêè ïàêåòà " + e.ToString());
            }
            return false;
        }
        ////////////////////////////////////////////////////////////////////////
        ////$0128,861107030077201,O,000943837056,DL2C8888,79.013941,79.013941,65535,32768,000,55,25 ,200,6=0,4=12,7=0,5="Reliance Reliance",117081~
        //// 0   , 1             ,2,3           ,4       ,5        ,6        ,7    ,8    ,8  ,9 ,10 ,11 ,12
        public bool ProcessPacket(string str)
        {            

            string[] slices = str.Split(',');  //÷àñòè ñîîáùåíèÿ

            if (m_IMEI == 0)
                if (!long.TryParse(slices[1], out m_IMEI) || !IMEIIsValid())
                    return false;

            TrackerPacket packet = new TrackerPacket(m_IMEI);
            packet.m_Time = int.Parse(slices[3]);

            packet.m_SatteliteCount = byte.Parse(slices[11]);

            if (slices[5].Length == 9)
                packet.m_fLat = (int.Parse(slices[5]));
            if (slices[6].Length == 10)
                packet.m_fLng = (int.Parse(slices[6]));
            if (slices[7].Length > 0)
                packet.m_Speed = (ushort)(float.Parse(slices[7], m_Format));
            if (slices[8].Length > 0)
                packet.m_Alt = (short.Parse(slices[8]));
            if (slices[9].Length > 0)
                packet.m_Direction = (ushort)(float.Parse(slices[9], m_Format));
            for (int i = 12; i < slices.Length; i++)
            {
                if (slices[i].Split('=')[0].Equals("15"))
                {
                    packet.SetInput("DIN1",int.Parse(slices[i].Split('=')[1]));
                }
                if (slices[i].Split('=')[0].Equals("16"))
                {
                    packet.SetInput("DIN2", int.Parse(slices[i].Split('=')[1]));
                }
                if (slices[i].Split('=')[0].Equals("8"))
                {
                    packet.SetInput("BREAK_ACCEL",int.Parse(slices[i].Split('=')[1]));
                }
                if (slices[i].Split('=')[0].Equals("9")) {
                    packet.SetInput("ACCEL", int.Parse(slices[i].Split('=')[1]));
                }
                if (slices[i].Split('=')[0].Equals("12"))
                {
                    packet.SetInput("PWR", int.Parse(slices[i].Split('=')[1]));
                }
                //packet.SetInput("ODO", float.Parse(slices[18], m_Format));
                //packet.SetInput("ACC", float.Parse(slices[20], m_Format));
            }            
            return PushPacket(packet);
        }
    }
}
