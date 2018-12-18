using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;

////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////
class ConcoxServer : AsyncServer
{
    public ConcoxServer(IPAddress ipAddres, int nPort): base(ipAddres, nPort)
    {

    }
    ////////////////////////////////////////////////////////////////////////
    public override ServerSession CreateSession(Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
    {
        return new ConcoxSession(++m_nSessionCounter, socket, GPSQueue, logger);
    }
    //////////////////////////////////////////////////////////////////////////////////
    public override string ToString() { return "Concox"; }
    //////////////////////////////////////////////////////////////////////////////////
};
////////////////////////////////////////////////////////////////////////
//
////////////////////////////////////////////////////////////////////////
class ConcoxSession : ServerSession
{
    float   m_fAcc = -1;
    float   m_fPwr = -1;
    float   m_fDoor = -1;
    int     m_iStatus = 0;
    int     m_iAlarm = 0;
    byte    m_LastStatusByte = 0;
    ////////////////////////////////////////////////////////////////////////
    public ConcoxSession(uint nID, Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger)
        : base(nID, socket, GPSQueue, logger)
    {
    }
    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////
    public override bool ProcessBuffer(byte[] buffer, int size)      //если вернет false - разорвать соединение
    {
        string str = "<<";
        for (int i = 0; i < size; i++)
            str += " " + buffer[i].ToString("X2");
        LogEvent(LogLevel.DEBUG, str);

        int from = 0;
        while (from < size)
        {
            if (!ProcessRecord(buffer, ref from))
                return false;
        }
        return true;
    }
    ////////////////////////////////////////////////////////////////////////
    bool ProcessRecord(byte[] buffer, ref int from)
    {
        bool len2bytes = buffer[from] == 0x79;
        if ((buffer[from] != 0x78 && buffer[from] != 0x79) || buffer[from + 1] != buffer[from])
            return false;
        from +=2;

        int len = len2bytes ? (BIConverter.ToUInt16(buffer, from)) : buffer[from];
        from += len2bytes ? 2 : 1;

        if (from + len + 2 > buffer.Length)
            return false;

        if (buffer[from + len] != 13 || buffer[from + len + 1] != 10)
            return false;

        int crc = GetCrc16(buffer, from - (len2bytes ? 2 : 1), len - 2 + (len2bytes ? 2 : 1));
        if (crc != BIConverter.ToUInt16(buffer, from + len - 2))
            return false;

        byte pn = buffer[from++];
        int sn = BIConverter.ToUInt16(buffer, from + len - 5);
        switch (pn)
        {
            case 1://login
            {
                m_IMEI = GetNumberFromHex(buffer, from, 8);
                if (!IMEIIsValid())
                    return false;

                byte[] reply = new byte[10] { 0x78, 0x78, 0x05, pn, (byte)(sn >> 8), (byte)sn, 0x00, 0x00, 0x0D, 0x0A };
                crc = GetCrc16(reply, 2, reply.Length - 6);
                reply[reply.Length - 4] = (byte)(crc >> 8);
                reply[reply.Length - 3] = (byte)crc;
                Send(reply, reply.Length);
                break;
            }
            case 0x10://GPS
            {
                TrackerPacket packet = new TrackerPacket(m_IMEI);
                if (buffer[from] > 0)
                    packet.m_Time = CTime.GetTime(new DateTime(2000 + buffer[from], buffer[from + 1], buffer[from + 2], buffer[from + 3], buffer[from + 4], buffer[from + 5]));
                packet.m_SatteliteCount = (byte)(((buffer[from + 16] & 16) == 0) ? 0 : buffer[from + 6] & 0x0F);
                packet.m_fLat = (float)(BIConverter.ToUInt32(buffer, from + 7) / 1800000.0 * (((buffer[from + 16] & 4) == 0) ? -1 : 1));
                packet.m_fLng = (float)(BIConverter.ToUInt32(buffer, from + 11) / 1800000.0 * (((buffer[from + 16] & 8) == 0) ? 1 : -1));
                packet.m_Speed = buffer[from + 15];
                ushort dir = BIConverter.ToUInt16(buffer, from + 16);
                packet.m_Direction = (ushort)(dir & 0x3FF);

                PutExtData2Packet(packet);
                
                if (!PushPacket(packet))
                    return false; 
                break;
            }
            case 0x12://location data packet
            {
                TrackerPacket packet = new TrackerPacket(m_IMEI);
                if (buffer[from] > 0)
                    packet.m_Time = CTime.GetTime(new DateTime(2000 + buffer[from], buffer[from + 1], buffer[from + 2], buffer[from + 3], buffer[from + 4], buffer[from + 5]));
                packet.m_SatteliteCount = (byte)(((buffer[from + 16] & 16) == 0) ? 0 : buffer[from + 6] & 0x0F);
                packet.m_fLat = (float)(BIConverter.ToUInt32(buffer, from + 7) / 1800000.0 * (((buffer[from + 16] & 4) == 0) ? -1 : 1));
                packet.m_fLng = (float)(BIConverter.ToUInt32(buffer, from + 11) / 1800000.0 * (((buffer[from + 16] & 8) == 0) ? 1 : -1));
                packet.m_Speed = buffer[from + 15];
                ushort dir = BIConverter.ToUInt16(buffer, from + 16);
                packet.m_Direction = (ushort)(dir & 0x3FF);

                PutExtData2Packet(packet);

                if (!PushPacket(packet))
                    return false;
                break;
            }

            case 0x13://Status package
            {
                m_LastStatusByte = buffer[from];
                switch (buffer[from + 1])
                {
                    case 1: m_fAcc = 5; break;
                    case 2: m_fAcc = 10; break;
                    case 3: m_fAcc = 30; break;
                    case 4: m_fAcc = 50; break;
                    case 5: m_fAcc = 75; break;
                    case 6: m_fAcc = 100; break;
                    default: m_fAcc = 0; break;
                }

                byte[] reply = new byte[10] { 0x78, 0x78, 0x05, pn, (byte)(sn >> 8), (byte)sn, 0x00, 0x00, 0x0D, 0x0A };
                crc = GetCrc16(reply, 2, reply.Length - 6);
                reply[reply.Length - 4] = (byte)(crc >> 8);
                reply[reply.Length - 3] = (byte)crc;
                Send(reply, reply.Length);
                break;
            }
            case 0x15://command reply
            {
                string str = Encoding.ASCII.GetString(buffer, from + 5, buffer[from] - 4);
                break;
            }

            case 0x16://GPS LBS status combined package
            {
                byte[] reply = new byte[15] { 0x78, 0x78, 11, pn, 0, 0x00, 0x00, 0x00, 0x01, (byte)(sn >> 8), (byte)sn, 0, 0, 0x0D, 0x0A };
                crc = GetCrc16(reply, 2, reply.Length - 6);
                reply[reply.Length - 4] = (byte)(crc >> 8);
                reply[reply.Length - 3] = (byte)crc;
                Send(reply, reply.Length);

                TrackerPacket packet = new TrackerPacket(m_IMEI);
                if (buffer[from] > 0)
                    packet.m_Time = CTime.GetTime(new DateTime(2000 + buffer[from], buffer[from + 1], buffer[from + 2], buffer[from + 3], buffer[from + 4], buffer[from + 5]));
                packet.m_SatteliteCount = (byte)(((buffer[from + 16] & 16) == 0) ? 0 : buffer[from + 6] & 0x0F);
                packet.m_fLat = (float)(BIConverter.ToUInt32(buffer, from + 7) / 1800000.0 * (((buffer[from + 16] & 4) == 0) ? -1 : 1));
                packet.m_fLng = (float)(BIConverter.ToUInt32(buffer, from + 11) / 1800000.0 * (((buffer[from + 16] & 8) == 0) ? 1 : -1));
                packet.m_Speed = buffer[from + 15];
                ushort dir = BIConverter.ToUInt16(buffer, from + 16);
                packet.m_Direction = (ushort)(dir & 0x3FF);

                byte alarm = buffer[from + 30];
                if (alarm == 1)//SOS
                {
                    m_iAlarm = 1;
                }
                PutExtData2Packet(packet);
                m_iAlarm = 0;

                if (!PushPacket(packet))
                    return false;
                break;
            }
            case 0x17://LBS telephone number address searching package
            {
                byte[] reply = new byte[15] { 0x78, 0x78, 11, pn, 0, 0x00, 0x00, 0x00, 0x01, (byte)(sn >> 8), (byte)sn , 0, 0, 0x0D, 0x0A };
                crc = GetCrc16(reply, 2, reply.Length - 6);
                reply[reply.Length - 4] = (byte)(crc >> 8);
                reply[reply.Length - 3] = (byte)crc;
                Send(reply, reply.Length);
                m_Logger.Push(LogLevel.DEBUG, 0, "LBS telephone number address searching package");
                break;
            }
            case 0x18://LBS extension package
            {
                break;
            }
            case 0x19://LBS status combined package
            {
                //StoreEvent(CEvent.EventType.ALARM, CTime.GetTime(DateTime.UtcNow), "");

                byte[] reply = new byte[15] { 0x78, 0x78, 11, pn, 0, 0x00, 0x00, 0x00, 0x01, (byte)(sn >> 8), (byte)sn , 0, 0, 0x0D, 0x0A };
                crc = GetCrc16(reply, 2, reply.Length - 6);
                reply[reply.Length - 4] = (byte)(crc >> 8);
                reply[reply.Length - 3] = (byte)crc;
                Send(reply, reply.Length);
                m_Logger.Push(LogLevel.DEBUG, 0, "LBS status combined package");
                break;
            }
            case 0x1A://GPS telephone number address searching package
            {
                byte[] reply = new byte[15] { 0x78, 0x78, 11, pn, 0, 0x00, 0x00, 0x00, 0x01, (byte)(sn >> 8), (byte)sn , 0, 0, 0x0D, 0x0A };
                crc = GetCrc16(reply, 2, reply.Length - 6);
                reply[reply.Length - 4] = (byte)(crc >> 8);
                reply[reply.Length - 3] = (byte)crc;
                Send(reply, reply.Length);
                m_Logger.Push(LogLevel.DEBUG, 0, "GPS telephone number address searching package");
                break;
            }
            case 0x1F://Time Synchronization packets 
            {
                long time = CTime.GetTime(DateTime.UtcNow);
                byte[] reply = new byte[14] { 0x78, 0x78, 10, pn, (byte)(time >> 24), (byte)(time >> 16), (byte)(time >> 8), (byte)(time), (byte)(sn >> 8), (byte)sn , 0, 0, 0x0D, 0x0A };
                crc = GetCrc16(reply, 2, reply.Length - 6);
                reply[reply.Length - 4] = (byte)(crc >> 8);
                reply[reply.Length - 3] = (byte)crc;
                Send(reply, reply.Length);
                m_Logger.Push(LogLevel.DEBUG, 0, "Time Synchronization package ");
                break;
            }
            case 0x80://Command Package
            case 0x81://
            {
                //need reply
                break;
            }
            case 0x8a://?????
            {
                break;
            }
            case 0x94://?????
            {
                byte id = buffer[from];
                int k = from + 1;
                switch (id)
                {
                    case 0x00://power
                    {
                        m_fPwr = (float)(BIConverter.ToUInt16(buffer, k) / 100.0);
                        k += 2;
                        break;
                    }
                    case 0x04://ascii content ALM1=75;ALM2=D5;ALM3=5F;STA1=40;DYD=01;SOS=+917226938881,+919904038880,+917226038882;CENTER=;FENCE=Fence,OFF,0,0.000000,0.000000,300,IN or OUT,1;
                    {
                        string str = Encoding.ASCII.GetString(buffer, k, len - 6);
                        LogEvent(LogLevel.DEBUG, "0x94:     " + str);

                        string[] parts = str.Split(new char[]{';'});
                        for (int i = 0; i < parts.Length; i++)
                        {
                            string[] s = parts[i].Split(new char[] { '=' });
                            if (s.Length == 2)
                                switch (s[0])
                                {
                                    /*case "ALM1":
                                        m_iAlarm = m_iAlarm & 0xFFFF00;
                                        m_iAlarm = m_iAlarm | int.Parse(s[1], System.Globalization.NumberStyles.HexNumber, null);
                                        break;
                                    case "ALM2":
                                        m_iAlarm = m_iAlarm & 0xFF00FF;
                                        m_iAlarm = m_iAlarm | (int.Parse(s[1], System.Globalization.NumberStyles.HexNumber, null) << 8);
                                        break;
                                    case "ALM3":
                                        m_iAlarm = m_iAlarm & 0x00FFFF;
                                        m_iAlarm = m_iAlarm | (int.Parse(s[1], System.Globalization.NumberStyles.HexNumber, null) << 16);
                                        break;*/
                                    case "STA1":
                                        m_iStatus = m_iStatus & 0xFFFF00;
                                        m_iStatus = m_iStatus | int.Parse(s[1], System.Globalization.NumberStyles.HexNumber, null);
                                        break;
                                    case "DYD":
                                        m_iStatus = m_iStatus & 0xFF00FF;
                                        m_iStatus = m_iStatus | (int.Parse(s[1], System.Globalization.NumberStyles.HexNumber, null) << 8);
                                        break;
                                }
                        }


                        k += len - 6;
                        break;
                    }

                    case 0x05://door status
                    {
                        m_fDoor = (float)buffer[k];
                        k += 1;
                        break;
                    }
                    default: 
                        break;
                }
                break;
            }

            default:
            {
                m_Logger.Push(LogLevel.DEBUG, 0, "unknown package " + pn);
                break;
            }
        }
        from += len - 1;//13 10
        from += 2;//13 10
        return true;
    }
    void PutExtData2Packet(TrackerPacket packet)
    {
        if (Math.Abs(CTime.GetTime(DateTime.UtcNow) - packet.m_Time) < 600)
        {
            if (m_fAcc >= 0)
            {
                packet.SetInput("ACC", m_fAcc);

                packet.SetInput("DIN1", ((m_LastStatusByte & 2) != 0) ? 1 : 0);//Ignition
                packet.SetInput("DIN2", ((m_LastStatusByte & 128) != 0) ? 1 : 0);
            }
            if (m_fPwr >= 0)
                packet.SetInput("PWR", m_fPwr);
            if (m_fDoor >= 0)
                packet.SetInput("IN1", m_fDoor);
            packet.SetInput("STATUS", m_iStatus);
        }
        packet.SetInput("ALARM", m_iAlarm);
    }

    UInt16[] crctab16 = new UInt16[]{
        0x0000, 0x1189, 0x2312, 0x329b, 0x4624, 0x57ad, 0x6536, 0x74bf,
        0x8c48, 0x9dc1, 0xaf5a, 0xbed3, 0xca6c, 0xdbe5, 0xe97e, 0xf8f7,
        0x1081, 0x0108, 0x3393, 0x221a, 0x56a5, 0x472c, 0x75b7, 0x643e,
        0x9cc9, 0x8d40, 0xbfdb, 0xae52, 0xdaed, 0xcb64, 0xf9ff, 0xe876,
        0x2102, 0x308b, 0x0210, 0x1399, 0x6726, 0x76af, 0x4434, 0x55bd,
        0xad4a, 0xbcc3, 0x8e58, 0x9fd1, 0xeb6e, 0xfae7, 0xc87c, 0xd9f5,
        0x3183, 0x200a, 0x1291, 0x0318, 0x77a7, 0x662e, 0x54b5, 0x453c,
        0xbdcb, 0xac42, 0x9ed9, 0x8f50, 0xfbef, 0xea66, 0xd8fd, 0xc974,
        0x4204, 0x538d, 0x6116, 0x709f, 0x0420, 0x15a9, 0x2732, 0x36bb,
        0xce4c, 0xdfc5, 0xed5e, 0xfcd7, 0x8868, 0x99e1, 0xab7a, 0xbaf3,
        0x5285, 0x430c, 0x7197, 0x601e, 0x14a1, 0x0528, 0x37b3, 0x263a,
        0xdecd, 0xcf44, 0xfddf, 0xec56, 0x98e9, 0x8960, 0xbbfb, 0xaa72,
        0x6306, 0x728f, 0x4014, 0x519d, 0x2522, 0x34ab, 0x0630, 0x17b9,
        0xef4e, 0xfec7, 0xcc5c, 0xddd5, 0xa96a, 0xb8e3, 0x8a78, 0x9bf1,
        0x7387, 0x620e, 0x5095, 0x411c, 0x35a3, 0x242a, 0x16b1, 0x0738,
        0xffcf, 0xee46, 0xdcdd, 0xcd54, 0xb9eb, 0xa862, 0x9af9, 0x8b70,
        0x8408, 0x9581, 0xa71a, 0xb693, 0xc22c, 0xd3a5, 0xe13e, 0xf0b7,
        0x0840, 0x19c9, 0x2b52, 0x3adb, 0x4e64, 0x5fed, 0x6d76, 0x7cff,
        0x9489, 0x8500, 0xb79b, 0xa612, 0xd2ad, 0xc324, 0xf1bf, 0xe036,
        0x18c1, 0x0948, 0x3bd3, 0x2a5a, 0x5ee5, 0x4f6c, 0x7df7, 0x6c7e,
        0xa50a, 0xb483, 0x8618, 0x9791, 0xe32e, 0xf2a7, 0xc03c, 0xd1b5,
        0x2942, 0x38cb, 0x0a50, 0x1bd9, 0x6f66, 0x7eef, 0x4c74, 0x5dfd,
        0xb58b, 0xa402, 0x9699, 0x8710, 0xf3af, 0xe226, 0xd0bd, 0xc134,
        0x39c3, 0x284a, 0x1ad1, 0x0b58, 0x7fe7, 0x6e6e, 0x5cf5, 0x4d7c,
        0xc60c, 0xd785, 0xe51e, 0xf497, 0x8028, 0x91a1, 0xa33a, 0xb2b3,
        0x4a44, 0x5bcd, 0x6956, 0x78df, 0x0c60, 0x1de9, 0x2f72, 0x3efb,
        0xd68d, 0xc704, 0xf59f, 0xe416, 0x90a9, 0x8120, 0xb3bb, 0xa232,
        0x5ac5, 0x4b4c, 0x79d7, 0x685e, 0x1ce1, 0x0d68, 0x3ff3, 0x2e7a,
        0xe70e, 0xf687, 0xc41c, 0xd595, 0xa12a, 0xb0a3, 0x8238, 0x93b1,
        0x6b46, 0x7acf, 0x4854, 0x59dd, 0x2d62, 0x3ceb, 0x0e70, 0x1ff9,
        0xf78f, 0xe606, 0xd49d, 0xc514, 0xb1ab, 0xa022, 0x92b9, 0x8330,
        0x7bc7, 0x6a4e, 0x58d5, 0x495c, 0x3de3, 0x2c6a, 0x1ef1, 0x0f78,
    };

        // calculate 16 bits CRC of the given length data.
    UInt16 GetCrc16(byte[] pData, int from, int count)
    {
        uint fcs = 0xffff; // Initialize
        for (int i = from; i < from + count; i++)
            fcs = (fcs >> 8) ^ crctab16[(fcs ^ pData[i]) & 0xff];
        return (UInt16)~fcs; // Negate
    }
    long GetNumberFromHex(byte[] buf, int from, int count)
    {
        string str = "";
        for (int i = from; i < from + count; i++)
            str += buf[i].ToString("X2");
        return long.Parse(str);
    }
}
