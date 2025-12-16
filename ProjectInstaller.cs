using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace DatabaseBackupService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;
        public ProjectInstaller()
        {
            InitializeComponent();
            processInstaller = new ServiceProcessInstaller
            {
                // Run the service under the local system account
                Account = ServiceAccount.LocalSystem
            };

            // Initialize ServiceInstaller
            serviceInstaller = new ServiceInstaller
            {
                // Set the name of the service
                ServiceName = "DatabaseBackupService",
                DisplayName = "Database Backup Service",
                Description = "A Windows Service to backup SQL DB.",
                StartType = ServiceStartMode.Automatic,
                ServicesDependedOn = new string[] { "RPCSS", "EventLog", "MSSQLSERVER" } //Dependencies
            };

            // Add both installers to the Installers collection
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
