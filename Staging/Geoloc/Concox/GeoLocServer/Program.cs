using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading; 
using System.IO;
using System.Collections;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Locanix Geoloc\n");
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
        ArrayList servers = new ArrayList();

        Configuration cfg = Configuration.inst();

        new LogsCleaner(Global.m_Logger.GetQueue("LogsCleaner"));

        ArrayList ports = GetPorts(cfg.Concox);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new ConcoxServer(cfg.GetIP(), (ushort)ports[i]));

        ports = GetPorts(cfg.Atlanta);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new AtlantaServer(cfg.GetIP(), (ushort)ports[i]));
        ports = GetPorts(cfg.Visiontek);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new VisiontekServer(cfg.GetIP(), (ushort)ports[i]));

        ports = GetPorts(cfg.ATrack);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new ATrackServer(cfg.GetIP(), (ushort)ports[i]));

        ports = GetPorts(cfg.ATrackUDP);
        for (int i = 0; i < ports.Count; i++)
            servers.Add(new ATrackUDPServer(cfg.GetIP(), (ushort)ports[i]));
        Console.ReadLine();
    }

    static void Log2DB(string filename, long IMEI)
    {
        //DateTime from = (new DateTime(2014, 01, 20, 14, 30, 0)).ToUniversalTime();
        //DateTime till = (new DateTime(2014, 01, 20, 15, 00, 0)).ToUniversalTime();

        //int ifrom = CTime.GetTime(from);
        //int itill = CTime.GetTime(till);

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
                Global.m_GPSQueue.PushPacket(packet);
                Thread.Sleep(5);
            }
        }        
        Global.m_Logger.FlushData();
        Console.WriteLine("Ready");
    }    
}
