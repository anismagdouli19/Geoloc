using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;
using System.ComponentModel;

[RunInstaller(true)]
public class DataForwarderInstaller : Installer
{
   
        private ServiceInstaller service;
        private ServiceProcessInstaller process;

        public DataForwarderInstaller()
        {
            // Instantiate installers for process and services.
            process = new ServiceProcessInstaller();
            service = new ServiceInstaller();

            // The services run under the system account.
            process.Account = ServiceAccount.LocalSystem;

            // The services are started manually.
            service.StartType = ServiceStartMode.Manual;

            // ServiceName must equal those on ServiceBase derived classes.
            service.ServiceName = "DataForwarder";

            // Add installers to collection. Order is not important.
            Installers.Add(service);
            Installers.Add(process);
        }
}