using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;

public class HttpJson: Connection
{
    public HttpJson(LogItemQueue logger, ServerCfg config): base(logger, config)
    {
        m_Logger.Push(LogLevel.WARNING, 0, "HTTP/JSON client started");

        ServicePointManager.DefaultConnectionLimit = 100;
    }

    string GetPacket(TrackerPacket point,int isResend)
    {
        decimal Alarm = 0;
        point.GetInput("ALARM", out Alarm);
        int timestamp,packettimestamp,servertimestamp;
        packettimestamp = point.m_Time;
        DateTime dt = CTime.GetTime(Convert.ToInt64(packettimestamp));
        servertimestamp=Convert.ToInt32(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        if (isResend>0)
        {
            timestamp = servertimestamp;                        
        }
        else
        {
            timestamp = packettimestamp;           
        }

        string str = "{\"deviceId\": \"" + m_Tracker.m_IMEI + "\", " +
                        "\"latitude\": \"" + point.m_fLat + "\", " +
                        "\"longitude\": \"" + point.m_fLng + "\", " +
                        "\"timeStamp\": \"" + timestamp + "\", " +                        
                        "\"speed\": \"" + CObject.Float2XML(point.m_Speed / 3.6) + "\", " +
                        "\"sos\": \"" + ((Alarm!=0) ? "True": "False" ) + "\", " +
                        "\"altitude\": \"" + point.m_Alt + "\", " +
                        "\"vehicleNo\": \"" + m_Tracker.m_strName + "\", " +
                        "\"isValid\": \"" + (point.IsFixed(false) ? "True" : "False") + "\", " +
                        "\"packetTimeStamp\": \"" +packettimestamp + "\"," +
                        "\"serverTimeStamp\": \"" + servertimestamp + "\""+
                        "}";
        return str;
    }
    ////////////////////////////////////////////////////////////////////////
    protected override int DoSession(TrackerPacketList pt,int reSend)
    {
        int ret = 0;
        StringBuilder str = new StringBuilder();
        str.Append("[");
        for (int i = 0; i < pt.Count; i++)
        {
            if (i > 0)
                str.Append(", ");
            str.Append(GetPacket(pt[i],reSend));
        }
        str.Append("]");
        m_Logger.Push(LogLevel.INFO, 0, pt[0].m_Speed > 0 ? "Running >> " + str : "Standing >> "+str);
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(m_config.URL);
        httpWebRequest.Method = "POST";
        httpWebRequest.Timeout = m_config.Timeout;

        byte[] ByteQuery = Encoding.UTF8.GetBytes(str.ToString());
        httpWebRequest.ContentLength = ByteQuery.Length;
        httpWebRequest.ContentType = "application/json";
        using (Stream QueryStream = httpWebRequest.GetRequestStream())
        {
            QueryStream.Write(ByteQuery, 0, ByteQuery.Length);
            QueryStream.Close();
        }

        using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
        {
            if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                ret = pt.Count;
            httpWebResponse.Close();
        }
        return ret;
    }
}