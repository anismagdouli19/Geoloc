using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace GPSSender
{
    class NagpurConnection:Connection
    {
        public NagpurConnection(LogItemQueue logger, ServerCfg config):base(logger, config)
        {
            m_Logger.Push(LogLevel.WARNING, 0, "Nagpur client started");
            m_Logger.Push(LogLevel.WARNING, 0, "Nagpur Data Should Received");
        }
        private string toNMEA(float longorlat)
        {
            string nmea = "";
            double lata = Math.Abs(longorlat);
            double latd = Math.Truncate(longorlat);
            double latm = (lata - latd) * 60;        
            nmea += latd.ToString("00") + latm.ToString("00.00000");
            return nmea;
        }
    //"$vehicle Id,timestamp,lat,long,speed,heading,ignition#"    
    private int checksum=0;
    private string res = "";
    string GetPacket(TrackerPacket point)
    {
        DateTime dt = CTime.GetTime(point.m_Time);
        decimal odo = 0, pwr = 0, acc = 0,ign=0;
        point.GetInput("ODO", out odo);
        point.GetInput("ACC", out acc);
        point.GetInput("PWR", out pwr);
        point.GetInput("DIN1", out ign);            
        string IMEI,latitude,latDirection,longitude,longDirection,alt,speed,heading,date,time,valid;
        IMEI = m_Tracker.m_IMEI.ToString();        
        latitude = toNMEA(point.m_fLat);
        latDirection =((point.m_fLat >= 0) ? "N" : "S");
        longitude = toNMEA(point.m_fLng);
        longDirection = ((point.m_fLng >= 0) ? "E" : "W");
        speed =Convert.ToString(Convert.ToDouble(point.m_Speed/1.852));
        alt = point.m_Alt.ToString();
        heading=point.m_Direction.ToString();
        date = dt.ToString("ddMMyy");
        time = dt.ToString("HHmmss.fff");
       // valid=point.IsFixed(false)?"1":"0";
        valid = "1";
        if (res.Equals(""))
        {
            res = String.Format("${0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}#",
                                    IMEI, date, time, latitude, latDirection, longitude, longDirection, alt,
                                    speed, heading,valid, ign, "*74A");
        }
        string newchar="";
        checksum=getChecksum(res.ToCharArray());
        newchar += ((char)(((checksum >> 4) & 0xf) > 9 ? ((checksum) & 0xf) + 'A' - 10 : ((checksum >> 4) & 0xf) + '0'));
        newchar += (char)((checksum & 0xf) > 9 ? (checksum & 0xf) + 'A' - 10 : ((checksum & 0xf)) + '0');
        res = String.Format("${0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}#",
        IMEI,date,time,latitude,latDirection,longitude,longDirection,alt,speed,heading,valid,ign,"*"+newchar);
        m_Logger.Push(LogLevel.INFO, 0, res);
        return res;
    }
    private int getChecksum(char[] sentence)
    {
        int i;
        //Start with first Item
        int checksum = 0;
        // Loop through all chars to get a checksum
        for (i = 1; i < sentence.Length; i++)
        {
            // No. XOR the checksum with this character's value
            checksum ^= Convert.ToByte(sentence[i]);
        }        
        // Return the checksum formatted as a two-character hexadecimal
        return checksum;
    }
    ////////////////////////////////////////////////////////////////////////
    protected override int DoSession(TrackerPacketList pt)//TCP ìåòîä
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
}
