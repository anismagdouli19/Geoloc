using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading; 
using System.IO;
using System.Collections;
using MySql.Data.MySqlClient;

class Program
{
    static void Main(string[] args)
    {
        //TestEvents.inst();

        Configuration.inst();

        ConfigMgr.inst();

        
        CurPosMgr.inst();
        LastPointMgr.inst();

        if (args.Length == 0)
        {
            bool tryCreateNewApp;  
            Mutex _inst = new Mutex(true, "geolocserver_dkfbsdmkbf", out tryCreateNewApp);  
            if (tryCreateNewApp)  
                Start();
        }
        else
        {
            if (args[0] == "config")
                Configuration.inst().SaveConfig();
            else
            {
                long IMEI = 0;
                if (args.Length > 1)
                    long.TryParse(args[1], out IMEI);
                Log2DB(args[0], IMEI);
            }
        }

        Global.m_Logger.FlushData();
    }

    static ArrayList GetPorts(string str)
    {
        string[] ports = str.Split();
        ArrayList res = new ArrayList();
        for (int i = 0; i < ports.Length; i++)
        {
            ushort port = 0;
            if (ushort.TryParse(ports[i], out port) && port > 0)
                res.Add(port);
        }
        return res;
    }
    static void Start()
    {
        new LogsCleaner(Global.m_Logger.GetQueue("LogsCleaner"));
        ArrayList servers = new ArrayList();
        Configuration cfg = Configuration.inst();
        ArrayList ports = GetPorts(cfg.Visiontek);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new VisiontekServer(cfg.GetIP(), (ushort)ports[i]));

        ports = GetPorts(cfg.Concox);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new ConcoxServer(cfg.GetIP(), (ushort)ports[i]));

        ports = GetPorts(cfg.Atlanta);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new AtlantaServer(cfg.GetIP(), (ushort)ports[i]));

        ports = GetPorts(cfg.ATrack);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new ATrackServer(cfg.GetIP(), (ushort)ports[i]));

        ports = GetPorts(cfg.ATrackUDP);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new ATrackUDPServer(cfg.GetIP(), (ushort)ports[i]));

        //AsyncServer AM120 = new AM120Server(cfg.GetIP(), 8800, Global.m_Logger.GetQueue("AM120"));*/

        Console.ReadLine();
    }

    static void Log2DB(string filename, long IMEI)
    {
        Configuration.inst().Events = false;
        DateTime from = (new DateTime(2016, 11, 01, 11, 30, 0)).ToUniversalTime();
        //DateTime till = (new DateTime(2014, 01, 20, 15, 00, 0)).ToUniversalTime();

        int iFrom = CTime.GetTime(from); //0;
        int iTill = int.MaxValue;// CTime.GetTime(till);

        int cnt = 0;
        Thread.Sleep(10000);
        StreamReader reader = new StreamReader(filename);
        while (!reader.EndOfStream)
        {
            string str = reader.ReadLine();
            TrackerPacket packet = new TrackerPacket(0);
            if (packet.TryParse(str))
            {
                if ((cnt++) % 1000 == 0)
                    Console.WriteLine(cnt + " " + str);

                if (IMEI != 0 && packet.m_ID != IMEI)
                    continue;

                if (packet.m_Time < iFrom || packet.m_Time > iTill)
                    continue;

                GPSPointQueue.inst().PushPacket(packet);
                if(packet.m_Time <CTime.GetTime(DateTime.UtcNow.AddMinutes(-3)) && packet.m_Speed>0){
                    try { 
                    using(CDatabase db = Configuration.inst().GetDB()){
                        using (MySqlCommand cmdSelect = new MySqlCommand("Select Name,Comment From Trackers where IMEI='"+IMEI+"'", db.connection))
                        {
                            MySqlDataReader mdr= cmdSelect.ExecuteReader();
                            Logger m_Logger = new Logger();
                            EmailSender.inst().SetLogger(m_Logger.GetQueue("Email"));
                            EmailSender.inst().AddMessage("zophop.alerts@locanix.com", mdr.GetString(0) + " from " + mdr.GetString(1) + " more than 3 minutes network delay", "Device With IMEI No. " + IMEI + " And Vehicle No. " + mdr.GetString(0) + " Has Netweork Delayed More Than 3 Minutes As Its Latest Packet`s Date and Time Is " + CTime.GetTime(packet.m_Time).ToString("dd-MM-yyyy HH:mm:ss") + " And Speed Is " + packet.m_Speed);
                            
                        }
                   }
                    }catch(Exception ex)
                    {
                        Console.Write("Email Has can't Be Sent "+ex.ToString());
                    }
                 }
                Thread.Sleep(1);
            }
        }

        Thread.Sleep(30000);
        Global.m_Logger.FlushData();
        Console.WriteLine("Ready");
    }
}
