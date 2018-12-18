//Ревизия 29.10.2012

using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Text;
//////////////////////////////////////////////////////////////////////////
public class TrackerPacket : CObject
{
    public long m_ID = 0;
    public int m_Time = 0;
    public float m_fLat = 0;
    public float m_fLng = 0;
    public short m_Alt = 0;
    public ushort m_Speed = 0;
    //public uint m_NTS = 0;
    public bool m_bOnline = false;

    public ulong m_ulMNID = 0;

    private uint m_Status = 0;
    private byte[] m_Inputs = null;
    
    public byte m_SatteliteCount
    {
        get 
        {
            uint val = m_Status & 0xFF;
            return (byte)((val < 128) ? val : 0); 
        }
        set { m_Status = (m_Status & 0xFFFFFF00) | (uint)value; }
    }
    public uint m_Accuracy
    {
        get
        {
            uint val = m_Status & 0xFF;
            return (val >= 128) ? (val & 0x7F) * 150 : 0;
        }
        set 
        {
            if (m_SatteliteCount == 0)
            {
                value = Math.Min(value / 150, 127);
                m_Status = (m_Status & 0xFFFFFF00) | (value | 128);
            }
        }
    }
    public ushort m_Direction
    {
        get { return (ushort)((m_Status >> 8) & 0xFFFF); }
        set { m_Status = (m_Status & 0xFF0000FF) | (((uint)value & 0xFFFF) << 8); }
    }

    //public void SetStatus(STATUS id, bool bVal)
    //{
    //    m_Status &= (uint)~(1 << (int)id);
    //    m_Status |= (uint)1 << (int)id;
    //}
    //public bool GetStatus(STATUS id) { return ((m_Status & (1 << (int)id)) != 0) ? true : false; }

    public enum IOFlags { LENGTH8 = 14, INTEGER = 15 };
    //public enum STATUS { GSMFIXED = 25 };

    ///////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////
    public TrackerPacket(long IMEI){m_ID = IMEI;}
    public TrackerPacket(){}
    ///////////////////////////////////////////////////////////////
    public void SetInput(string str, float fValue) { SetInput(str, false, BitConverter.GetBytes(fValue)); }
    public void SetInput(string str, double fValue) { SetInput(str, (float)fValue); }
    public void SetInput(string str, bool bValue) { SetInput(str, bValue ? 1 : 0); }
    public void SetInput(string str, int iValue) { SetInput(str, true, BitConverter.GetBytes(iValue)); }
   // public void SetInput(string str, double dValue) { SetInput(str, false, BitConverter.GetBytes(dValue)); }
    public void SetInput(string str, ulong lValue) { SetInput(str, true, BitConverter.GetBytes(lValue)); }
    ///////////////////////////////////////////////////////////////
    private void SetInput(string str, bool bInt, byte[] buf)
    {
        ushort IOID = IOChannelMgr.inst().GetIOChannel(str);
        if (IOID > 0)
        {
            int length = (m_Inputs != null) ? m_Inputs.Length : 0;
            byte[] bytes = new byte[length + buf.Length + 2];
            if (length > 0)
                m_Inputs.CopyTo(bytes, 0);

            if (buf.Length == 8)
                IOID = (ushort)(IOID | (1 << (int)IOFlags.LENGTH8));
            if (bInt)
                IOID = (ushort)(IOID | (1 << (int)IOFlags.INTEGER));

            BitConverter.GetBytes(IOID).CopyTo(bytes, length);
            buf.CopyTo(bytes, length + 2);
            m_Inputs = bytes;
        }
    }
    //////////////////////////////////////////////////////////////////////////
    public bool GetInput(string str, out decimal fValue)
    {
        if (str == "SPEED")
        {
            fValue = m_Speed;
            return true;
        }

        if (str == "DIRECTION")
        {
            fValue = m_Direction;
            return true;
        }

        if (str == "ALT")
        {
            fValue = m_Alt;
            return true;
        }

        if (m_Inputs != null)
        {
            int IOID = IOChannelMgr.inst().GetIOChannel(str);
            int i = 0;
            while (i < m_Inputs.Length)
            {
                int id = (int)BitConverter.ToUInt16(m_Inputs, i);
                bool b64bit = (id & (1 << (int)IOFlags.LENGTH8)) != 0;
                if ((id & 0xFFF) == IOID)
                {
                    if ((id & (1 << (int)IOFlags.INTEGER)) != 0)
                    {
                        if (b64bit) fValue = BitConverter.ToUInt64(m_Inputs, i + 2);
                        else        fValue = BitConverter.ToInt32(m_Inputs, i + 2);
                    }
                    else
                        fValue = (decimal)(b64bit ? BitConverter.ToDouble(m_Inputs, i + 2) : BitConverter.ToSingle(m_Inputs, i + 2));
                    return true;
                }
                i += 2 + (b64bit ? 8 : 4);
            }
        }
        fValue = 0;
        return false;
    }
    public byte[] GetIOBuffer(){return m_Inputs;}
    private void SetIOBuffer(byte[] buffer)
    {
        if (buffer != null && (buffer.Length % 2) != 0)//Тупая проверка
            buffer = null;
        m_Inputs = buffer;
    }
    ///////////////////////////////////////////////////////////////
    public override string ToString()
    {
        DateTime dt = CTime.GetTime(m_Time).ToLocalTime();
        string result = "IMEI=" + m_ID +
                        " Time=" + dt.ToString("dd.MM.yyyy-HH:mm:ss") +
                        " Lat=" + Float2XML(m_fLat) +
                        " Lng=" + Float2XML(m_fLng) +
                        " Alt=" + m_Alt +
                        " Dir=" + m_Direction +
                        " Speed=" + m_Speed +
                        " GSM=" + m_ulMNID +
                        " Sat=" + m_SatteliteCount;

        if (m_Inputs != null)
        {
            int i = 0;
            while (i < m_Inputs.Length)
            {
                int id = (int)BitConverter.ToUInt16(m_Inputs, i);
                bool b64bit = (id & (1 << (int)IOFlags.LENGTH8)) != 0;

                decimal fValue;
                if ((id & (1 << (int)IOFlags.INTEGER)) != 0)
                {
                    if (b64bit) fValue = BitConverter.ToUInt64(m_Inputs, i + 2);
                    else fValue = BitConverter.ToInt32(m_Inputs, i + 2);
                }
                else
                    fValue = (decimal)(b64bit ? BitConverter.ToDouble(m_Inputs, i + 2) : BitConverter.ToSingle(m_Inputs, i + 2));
                result += " " + IOChannelMgr.inst().GetIOChannel((ushort)(id & 0xFFF)) + "=" + Float2XML(fValue);

                i += 2 + (b64bit ? 8 : 4);
            }
        }
        return result;
    }
    ///////////////////////////////////////////////////////////////
    public string ToXML()
    {
        string result = "<point " +
                        "IMEI=\"" + m_ID + "\" " +
                        "Time=\"" + m_Time + "000\" " +
                        "Lat=\"" + Float2XML(m_fLat) + "\" " +
                        "Lng=\"" + Float2XML(m_fLng) + "\" " +
                        "Alt=\"" + m_Alt + "\" " +
                        "Speed=\"" + m_Speed + "\" " +
                        "Status=\"" + m_Status + "\" " +
                        "Sat=\"" + m_SatteliteCount + "\" " +
                        "GSM=\"" + m_ulMNID + "\" " +
                        "IO=\"";
        if (m_Inputs != null)
        {
            int i = 0;
            while (i < m_Inputs.Length)
            {
                int id = (int)BitConverter.ToUInt16(m_Inputs, i);
                bool b64bit = (id & (1 << (int)IOFlags.LENGTH8)) != 0;

                string strValue = "";
                if ((id & (1 << (int)IOFlags.INTEGER)) != 0)
                {
                    if (b64bit) strValue  = string.Format("{0:X2} ", BitConverter.ToUInt64(m_Inputs, i + 2));
                    else strValue = BitConverter.ToInt32(m_Inputs, i + 2).ToString();                    
                }
                else
                    strValue = Float2XML((decimal)(b64bit ? BitConverter.ToDouble(m_Inputs, i + 2) : BitConverter.ToSingle(m_Inputs, i + 2)));
                result += IOChannelMgr.inst().GetIOChannel((ushort)(id & 0xFFF)) + "=" + strValue + " ";
                i += 2 + (b64bit ? 8 : 4);
            }
        }
        return result + "\" />";
    }
    //////////////////////////////////////////////////////////////////////
    public bool Write2DB(MySqlCommand cmd)     //запись в БД
    {
        try
        {
            cmd.Parameters["?ID"].Value = m_ID;
            cmd.Parameters["?Time"].Value = m_Time;
            cmd.Parameters["?Lat"].Value = m_fLat;
            cmd.Parameters["?Lng"].Value = m_fLng;
            cmd.Parameters["?Alt"].Value = m_Alt;
            cmd.Parameters["?GSMInfo"].Value = m_ulMNID;
            cmd.Parameters["?Speed"].Value = m_Speed;
            cmd.Parameters["?Status"].Value = m_Status;
            cmd.Parameters["?IO"].Value = GetIOBuffer();            
            if (cmd.CommandText.ToLower().Contains("insert")) {
                cmd.Parameters["?ForwardTime"].Value =0;
                cmd.Parameters["?ReceivedTime"].Value =CTime.GetTime(DateTime.UtcNow);
            }
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Debug.Assert(false, ex.ToString());
            return false;
        }
        return true;
    }
    //////////////////////////////////////////////////////////////////////
    public override bool Load(MySqlDataReader reader)
    {
        try
        {
            m_ID = reader.GetInt64("TrackerID");
            m_Time = reader.GetInt32("Time");
            m_fLat = reader.GetFloat("Lat");
            m_fLng = reader.GetFloat("Lng");
            m_Alt = reader.GetInt16("Alt");
            m_Speed = reader.GetUInt16("Speed");
            m_Status = reader.GetUInt32("Status");
            m_ulMNID = reader.GetUInt64("GSMInfo");
            //m_NTS = reader.GetUInt32("NTS");
            byte[] buffer = null;
            int iopos = reader.GetOrdinal("IO");
            int len = Convert.IsDBNull(reader[iopos]) ? 0 : (int)reader.GetBytes(iopos, 0, null, 0, 0);
            if (len > 0)
            {
                buffer = new byte[len];
                reader.GetBytes(iopos, 0, buffer, 0, len);
            }
            SetIOBuffer(buffer);

            if (reader.FieldCount > 9)
                m_bOnline = reader.GetBoolean("Online");
        }
        catch (Exception e)
        {
            //Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }    
    
    public bool TryParse(string s)
    {
        if (s.IndexOf("IMEI") < 0)
            return false;
        s = s.Substring(s.IndexOf("IMEI"));
        s = s.Replace(" = ", "=");
        s = s.Replace(",", ".");

        string[] arr = s.Split(' ');
        if (arr.Length < 7)
            return false;
        
        NumberFormatInfo format = new NumberFormatInfo();
        format.NumberDecimalSeparator = ".";
        
        for (int i = 0; i < arr.Length; i++)
            if (arr[i].IndexOf("=") >=0)
            {
                string[] pair = arr[i].Split('=');
                switch (pair[0])
                {
                    case "IMEI":
                        if (!long.TryParse(pair[1], out m_ID))
                            return false;
                        break;
                    case "Time":
                        string[] digs = pair[1].Split(new char[]{'.', ':', '-' });
                        DateTime dt = new DateTime(int.Parse(digs[2]), int.Parse(digs[1]), int.Parse(digs[0]), int.Parse(digs[3]), int.Parse(digs[4]), int.Parse(digs[5]), DateTimeKind.Local);
                        m_Time = CTime.GetTime(dt.ToUniversalTime());
                        break;
                    case "Lat":
                        if (!float.TryParse(pair[1], NumberStyles.Float, format, out m_fLat))
                            return false;
                        break;
                    case "Lng":
                        if (!float.TryParse(pair[1], NumberStyles.Float, format, out m_fLng))
                            return false;
                        break;
                    case "Alt":
                        if (!short.TryParse(pair[1], out m_Alt))
                            return false;
                        break;
                    case "Speed":
                        if (!ushort.TryParse(pair[1], out m_Speed))
                            return false;
                        break;
                    case "Dir":
                        ushort dir = 0;
                        if (!ushort.TryParse(pair[1], out dir))
                            return false;
                        m_Direction = dir;
                        break;
                    case "Sat":
                        byte sat = 0;
                        if (!byte.TryParse(pair[1], out sat))
                            return false;
                        m_SatteliteCount = sat;
                        break;
                    case "GSM":
                        ulong GSMInfo = 0;
                        if (!ulong.TryParse(pair[1], out GSMInfo))
                            return false;
                        m_ulMNID = GSMInfo;
                        break;
                    default:
                        float f = 0;
                        if (!float.TryParse(pair[1], NumberStyles.Float, format, out f))
                            return false;
                        SetInput(pair[0], f);
                        break;
                }
            }
        return IsValid();
    }

    public bool IsFixed(bool bMayBeGSM)
    {
        return IsValid() && (m_SatteliteCount >= 3 || (bMayBeGSM && m_Accuracy > 0)) && m_Alt > -400 && m_Alt < 3000 && m_Speed >= 0 && m_Speed < 300 &&
                Math.Abs(m_fLat) <= 90 && Math.Abs(m_fLng) < 180 && Math.Abs(m_fLat) > 1 && Math.Abs(m_fLng) > 1;
    }
    public bool IsValid()
    {
        return (m_ID > 0) && (m_Time > 1262304000/*2010*/) && (m_Time < CTime.GetTime(DateTime.Now) + 3600 * 24);
    }
    //////////////////////////////////////////////////////////////////////
    public float GetDist(TrackerPacket point)
    {
        return GetDist(point.m_fLat, point.m_fLng);
    }
    //////////////////////////////////////////////////////////////////////
    static double grtorad = Math.PI / 180;
    public float GetDist(float lat, float lng)
    {
        double dlon = (lng - this.m_fLng) * 0.5 * grtorad;
        double dlat = (lat - this.m_fLat) * 0.5 * grtorad;
        double dlonSin = Math.Sin(dlon);
        double dlatSin = Math.Sin(dlat);

        double a = dlatSin * dlatSin +
                    Math.Cos(this.m_fLat * grtorad) * Math.Cos(lat * grtorad) * dlonSin * dlonSin;
        return (float)(12756274 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }
}
/// <summary>
/// //////////////////////////////////////////////////
/// </summary>
public class TrackerPacketList : CObjectList<TrackerPacket>
{
    public bool Delete(CDatabase db, CTracker tracker, int time)
    {
        try
        {
            using (MySqlCommand cmd = new MySqlCommand("DELETE FROM Points WHERE TrackerID=" + tracker.m_nID + " AND Time=" + time, db.connection))
                cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }
    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < Count; i++)
            result.Append(((TrackerPacket)this[i]).ToXML());
        return "<packets>" + result.ToString() + "</packets>";
    }
    public bool Load(CDatabase db, CTracker tracker, int TimeFrom, int TimeTill)
    {
        DateTime dtTime = DateTime.Now;
        if (Load(db, new DBQuery("SELECT TrackerID, Time, Lat, Lng, Alt, Speed, Status, IO, GSMInfo FROM points WHERE TrackerID = " + tracker.m_nID + " AND Time >= " + TimeFrom + " AND Time <= " + TimeTill)))
        {
            try
            {
                if (Count > 0)
                {
                    string pointQuery = "SELECT TrackerID, Time, Lat, Lng, Alt, Speed, Status, IO, GSMInfo FROM points WHERE TrackerID=" + tracker.m_nID + " AND Time>=" + (this[0].m_Time - tracker.m_Timeout) + " AND Time<" + this[0].m_Time + " ORDER BY Time DESC LIMIT 1";
                    using (MySqlCommand pointCmd = new MySqlCommand(pointQuery, db.connection))
                    {
                        TrackerPacket packet = new TrackerPacket(); 
                        using (MySqlDataReader reader = pointCmd.ExecuteReader())
                            if (reader.Read() && packet.Load(reader))
                            {
                                packet.m_Time = TimeFrom;
                                this.Insert(0, packet);
                            }
                    }

                    pointQuery = "SELECT TrackerID, Time, Lat, Lng, Alt, Speed, Status, IO, GSMInfo FROM points WHERE TrackerID=" + tracker.m_nID + " AND Time>" + this[this.Count - 1].m_Time + " AND Time<=" + (this[this.Count - 1].m_Time + tracker.m_Timeout) + " ORDER BY Time ASC LIMIT 1";
                    using (MySqlCommand pointCmd = new MySqlCommand(pointQuery, db.connection))
                    {
                        TrackerPacket packet = new TrackerPacket();
                        using (MySqlDataReader reader = pointCmd.ExecuteReader())
                            if (reader.Read() && packet.Load(reader))
                            {
                                packet.m_Time = TimeTill;
                                Add(packet);
                            }
                    }
                }
                return true;
            }
            catch (Exception ex) 
            { 
                Debug.Assert(false, ex.ToString()); 
            }
        }
        return false;
    }
    public new bool Load(CDatabase db, string strQuery)
    {
        return base.Load(db, new DBQuery(strQuery));//TODO: remove this
    }
}
