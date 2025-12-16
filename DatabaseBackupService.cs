using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace DatabaseBackupService
{
    public partial class DatabaseBackupService : ServiceBase
    {
        private Timer _BackupTimer;


        private string _BackupFolder;
        private string _LogFolder;
        private int _BackupIntervalMinutes;
        private string _ConnectionString;

        public DatabaseBackupService()
        {
            InitializeComponent();

            CanPauseAndContinue = true; //The service supports pausing and resuming operations.

            // Enable support for OnShutdown
            CanShutdown = true; // The service is notified when the system shuts down.


            // Read log directory path from App.config
            //The service reads the log directory path from an external configuration file (App.config) for flexibility.

            _ConnectionString = ConfigurationManager.AppSettings["ConnectionString"];
            _BackupFolder = ConfigurationManager.AppSettings["BackupFolder"];
            _LogFolder = ConfigurationManager.AppSettings["LogFolder"];
            _BackupIntervalMinutes =int.TryParse( ConfigurationManager.AppSettings["BackupIntervalMinutes"] , out int interval )? interval : 60; // Default to 60 minutes if not specified or invalid
            

            if (string.IsNullOrWhiteSpace(_BackupFolder))
                _BackupFolder = @"C:\DatabaseBackups";

            if (string.IsNullOrWhiteSpace(_LogFolder))
                _LogFolder = @"C:\DatabaseBackups\Logs";
            CreateDirectory(_BackupFolder);
            CreateDirectory(_LogFolder);

            _LogFolder = Path.Combine(_LogFolder, "log.txt");


        }
        private void CreateDirectory(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            catch (Exception ex)
            { //For invalid path or permission issues
                throw new ConfigurationErrorsException($"{directoryPath}  path is invalid .", ex);
            }
        }
        private void LogServiceEvent(string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(_LogFolder, logMessage);
            if (Environment.UserInteractive)
            {
                Console.WriteLine(logMessage);
            }
        }
        public void BackupDB()
        {
            try
            {
                string TimesTamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string BackupFile = Path.Combine(_BackupFolder, "Backup_" + TimesTamp + ".bak"); 

                using (SqlConnection conn = new SqlConnection(_ConnectionString))
                {
                    conn.Open();
                    string DatabaseName = conn.Database;

                    string Query =
                        "BACKUP DATABASE [" + DatabaseName + "] " +
                        "TO DISK = '" + BackupFile + "' " +
                        "WITH INIT, FORMAT";

                    using (SqlCommand cmd = new SqlCommand(Query, conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }
                }

                LogServiceEvent("Database backup successful: " + BackupFile);
            }
            catch (Exception ex)
            {
                LogServiceEvent("Error during backup: " + ex.Message);
            }

        }
        //private void TimerElapsed(object sender, ElapsedEventArgs e)
        //{
        //    BackupDB();
        //}
        protected override void OnStart(string[] args)
        {
            LogServiceEvent("Service Started.");

            //_Timer = new Timer(_BackupIntervalMinutes * 60 * 1000);
            //_Timer.Elapsed += TimerElapsed;
            //_Timer.Start();

            _BackupTimer = new Timer(
                callback: state => BackupDB(),
                state: null,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromMinutes(_BackupIntervalMinutes)
                );

            LogServiceEvent($"Backup interval initiated every: {_BackupIntervalMinutes} minutes.");

        }

        protected override void OnStop()
        {
            _BackupTimer?.Dispose();
            LogServiceEvent("Service Stopped.");
        }
        public void StartInConsole()
        {
            OnStart(null); // Trigger OnStart logic
            Console.WriteLine("Press Enter to stop the service...");
            Console.ReadLine();
            OnStop(); // Trigger OnStop logic
            Console.ReadKey();
        }
    }
}
