using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.ServiceProcess;
using System.Threading;
namespace GPSSender
{
    class Program
    {
        public const string ServiceName = "DataForwarder";        
        private static Thread t1;
        public class Service : ServiceBase
        {
            LogItemQueue m_Logger;
            private System.ComponentModel.IContainer components = null;

            protected override void Dispose(bool disposing)
            {
                if (disposing && (components != null))
                {
                    components.Dispose();
                }
                base.Dispose(disposing);
            }
            
            private void InitializeComponent()
            {
                components = new System.ComponentModel.Container();
                this.ServiceName = Program.ServiceName;
            }
            public Service()
            {
                InitializeComponent();                 
            }

            protected override void OnStart(string[] args)
            {
                string servicestarted = Environment.NewLine + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " " + "Service Has Been Started";
                System.IO.File.AppendAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/log/ServiceStartStop.log", servicestarted);
                if (t1 == null)
                {
                    t1 = new Thread(new ThreadStart(run));
                }
                t1.Start();                
            }

            protected override void OnStop()
            {                
                t1.Interrupt();
                t1.Join(0);
                t1.Abort();
                t1 = null;
                string servicestarted = Environment.NewLine + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " " + "Service Has Been Stopped";
                System.IO.File.AppendAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/log/ServiceStartStop.log", servicestarted);
            }
        }
        static void run()
        {
            decimal key = 0;
            string str = ((key > 0) ? ((long)key).ToString("X12") : "") + ";";

            Logger m_Logger = new Logger();
            EmailSender.inst().SetLogger(m_Logger.GetQueue("Email"));
            //Configuration.inst().SaveConfig();

            for (int i = 0; i < Configuration.inst().Servers.Count; i++)
                Configuration.inst().Servers[i].Start(m_Logger.GetQueue("Server" + Configuration.inst().Servers[i].UID));            
        }
        static void Main(string[] args)
        {            

         //  run();
         //  Console.ReadLine();
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
           { 
               new Service() 
            };
           ServiceBase.Run(ServicesToRun);
        }        
    }
}