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