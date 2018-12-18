using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;

public class _MD5
{
    public static string GetHash(string str)
    {
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
        string result = "";
        for (int i = 0; i < hash.Length; i++)
            result += hash[i].ToString("X2");
        return result;
    }
}

public class CTime
{
    static DateTime m_dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static int GetTime(DateTime dt)
    {
        return (int)((dt.Ticks - m_dt1970.Ticks) / 10000000);
    }
    public static int GetTime(string date, string time)
    {
        try
        {
            if (date != null)
            {
                if (date.Length == 6 && time.Length >= 6)
                    return GetTime(new DateTime(int.Parse(date.Substring(4, 2)) + 2000, int.Parse(date.Substring(2, 2)), int.Parse(date.Substring(0, 2)), int.Parse(time.Substring(0, 2)), int.Parse(time.Substring(2, 2)), int.Parse(time.Substring(4, 2)), DateTimeKind.Utc));
            }
            else
                if (time.Length >= 6)
                    return GetTime(new DateTime(1970, 1, 1, int.Parse(time.Substring(0, 2)), int.Parse(time.Substring(2, 2)), int.Parse(time.Substring(4, 2)), DateTimeKind.Utc));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }

        return 0;
    }
    public static DateTime GetTime(long dt)
    {
        return m_dt1970.AddMilliseconds(dt * 1000);
    }
}
//////////////////////////////////////////////////////////////////////////
public class BIConverter
{
    static public Single ToSingle(byte[] buffer, int offset)
    {
        byte[] bytes = new byte[4];
        for (int i = 0; i < 4; i++)
            bytes[i] = buffer[3 - i + offset];

        return BitConverter.ToSingle(bytes, 0);
    }
    static public Int16 ToInt16(byte[] buffer, int offset)
    {
        int tmp = 0;
        for (int i = 0; i < 2; i++)
            tmp = tmp * 256 + buffer[offset + i];

        return (Int16)tmp;
    }
    static public UInt16 ToUInt16(byte[] buffer, int offset)
    {
        uint tmp = 0;
        for (int i = 0; i < 2; i++)
            tmp = tmp * 256 + buffer[offset + i];

        return (UInt16)tmp;
    }
    static public Int32 ToInt32(byte[] buffer, int offset)
    {
        int tmp = 0;
        for (int i = 0; i < 4; i++)
            tmp = tmp * 256 + buffer[offset + i];

        return tmp;
    }
    static public UInt32 ToUInt32(byte[] buffer, int offset)
    {
        uint tmp = 0;
        for (int i = 0; i < 4; i++)
            tmp = tmp * 256 + buffer[offset + i];

        return tmp;
    }
    static public Int64 ToInt64(byte[] buffer, int offset)
    {
        Int64 tmp = 0;
        for (int i = 0; i < 8; i++)
            tmp = tmp * 256 + buffer[offset + i];

        return tmp;
    }
    static public UInt64 ToUInt64(byte[] buffer, int offset)
    {
        UInt64 tmp = 0;
        for (int i = 0; i < 8; i++)
            tmp = tmp * 256 + buffer[offset + i];

        return tmp;
    }
    /*static public Int64 ToVarios(byte[] buffer, int offset, int size)
    {
        Int64 tmp = 0;
        for (int i = 0; i < size; i++)
            tmp = tmp * 256 + buffer[offset + i];
        
        if (size == 4 && tmp > int.MaxValue)
            tmp = tmp - 4294967296;

        return tmp;
    }*/

    static public byte[] GetBytes(long data)
    {
        int count = 8;
        byte[] res = BitConverter.GetBytes(data);
        for (int i = 0; i < count / 2; i++)
        {
            byte tmp = res[i];
            res[i] = res[count - i - 1];
            res[count - i - 1] = tmp;
        }
        return res;
    }
    static public byte[] GetBytes(int data)
    {
        int count = 4;
        byte[] res = BitConverter.GetBytes(data);
        for (int i = 0; i < count / 2; i++)
        {
            byte tmp = res[i];
            res[i] = res[count - i - 1];
            res[count - i - 1] = tmp;
        }
        return res;
    }
    static public byte[] GetBytes(uint data)
    {
        int count = 4;
        byte[] res = BitConverter.GetBytes(data);
        for (int i = 0; i < count / 2; i++)
        {
            byte tmp = res[i];
            res[i] = res[count - i - 1];
            res[count - i - 1] = tmp;
        }
        return res;
    }
    static public byte[] GetBytes(ushort data)
    {
        int count = 2;
        byte[] res = BitConverter.GetBytes(data);
        for (int i = 0; i < count / 2; i++)
        {
            byte tmp = res[i];
            res[i] = res[count - i - 1];
            res[count - i - 1] = tmp;
        }
        return res;
    }
}

//////////////////////////////////////////////////////////////////////////
public class BCDConverter
{
    static public int Parse(byte BCD)
    {
        int res = 0;
        res = (BCD >> 4) * 10 + (BCD & 0xF);
        return res;
    }
    static public int Parse(uint BCD)
    {
        int res = 0;
        for (int i = 0; i < 4; i++)
            res = res * 100 + Parse((byte)((BCD >> (i * 8)) & 0xFF));
        return res;
    }
    static public int Parse(byte[] buf, int from, int count)
    {
        int res = 0;
        for (int i = 0; i < count; i++)
            res = res * 100 + Parse(buf[from + i]);
        return res;
    }
    /*static public int ToInt32(uint BCD)
    {
        int res = 0;
        for (int i = 7; i >= 0; i--)
            res = res * 10 + (uint)((BCD >> (i * 4)) & 0xF);
        return res;
    }*/
    static public byte[] GetBytes(int data, int len)
    {
        byte[] res = new byte[len];
        for (int i = res.Length - 1; i >= 0; i--)
        {
            byte val = (byte)(data % 10);  data = data / 10;
            res[i] = (byte)(val + ((data % 10) << 4)); data = data / 10;
        }
        return res;
    }
}
