using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Net;

public class Configuration
{
    public string SQLHost = "localhost";
    public string SQLDB = "GeoLoc";
    public string SQLLogin = "geoloc";
    public string SQLPassword = "geolocpwd";
    public string Concox = " ";
    public string Atlanta = " ";
    public string Visiontek = " ";
    public string ATrack = "";
    public string ATrackUDP = "";
    public bool Events = false;
    public bool AddDateToLog = false;

    protected Configuration()
    {
    }

    private static Configuration _inst = null;
    public static Configuration inst()
    {
        if (_inst == null)
        {
            Configuration cfg = new Configuration();
            cfg.LoadConfig();
        }
        return _inst;
    }

    public bool SaveConfig()
    {
        try
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using (StreamWriter SW = File.CreateText(path + "\\config.xml"))
            {
                XmlSerializer s = new XmlSerializer(typeof(Configuration));
                s.Serialize(SW, this);
            }

            return true;
        }
        catch (Exception e)
        {
        }
        return false;
    }

    private bool LoadConfig()
    {
        try
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using (StreamReader SW = File.OpenText(path + "\\config.xml"))
            {
                XmlSerializer s = new XmlSerializer(typeof(Configuration));
                _inst = (Configuration)s.Deserialize(SW);
            }

            Console.WriteLine("Config loaded succesfully");
            return true;
        }
        catch (Exception e)
        {
            _inst = this;
        }
        Console.WriteLine("Error reading config");
        return false;
    }

    public CDatabase GetDB()
    {
        return new CDatabase(SQLLogin, SQLPassword, SQLDB, SQLHost);
    }
    public IPAddress GetIP()
    {
        byte[] arr = new byte[] { 0, 0, 0, 0 };
        return new IPAddress(arr);
    }
}

