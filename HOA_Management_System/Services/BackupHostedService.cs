using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HOA_Management_System.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HOA_Management_System.Services
{
    public class BackupHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackupHostedService> _logger;
        private readonly IConfiguration _configuration;

        // --- UPDATE: Set to 4:30 PM (16:30) ---
        private readonly TimeSpan _scheduledTime = new TimeSpan(16, 30, 0);

        public BackupHostedService(IServiceProvider serviceProvider, ILogger<BackupHostedService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.Add(_scheduledTime);

                // If 4:30 PM has already passed today, schedule for tomorrow
                if (now > nextRun) nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;

                _logger.LogInformation($"Auto-Backup scheduled for {nextRun}. Waiting {delay.TotalHours:N2} hours.");

                // Wait until 4:30 PM
                await Task.Delay(delay, stoppingToken);

                // TIME TO BACKUP!
                await PerformAutomaticBackup();
            }
        }

        private async Task PerformAutomaticBackup()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HoaDbContext>();

                try
                {
                    // 1. DYNAMICALLY GET DATABASE NAME (Fixes "HOA_DB does not exist" error)
                    // We open the connection briefly to ensure the server name is resolved correctly
                    await context.Database.OpenConnectionAsync();
                    string dbName = context.Database.GetDbConnection().Database;
                    await context.Database.CloseConnectionAsync();

                    // --- ALIGNED FOLDER PATH HERE ---
                    string backupFolder = _configuration["BackupSettings:FolderPath"] ?? @"C:\Users\ako\Downloads\Needed\HOA_Backups";

                    if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

                    string fileName = $"Auto_HOA_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                    string fullPath = Path.Combine(backupFolder, fileName);

                    // 2. EXECUTE SQL BACKUP
                    string sqlCommand = $"BACKUP DATABASE [{dbName}] TO DISK = '{fullPath}' WITH FORMAT, INIT;";
                    await context.Database.ExecuteSqlRawAsync(sqlCommand);

                    // 3. CREATE LOG ENTRY
                    var log = new BackupLog
                    {
                        Backup_File_Name = fileName,
                        // Truncate milliseconds to avoid SQL datetime errors
                        Backup_Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second),
                        Performed_By = null, // <--- This MUST be allowed in the Database!
                        Backup_Type = "Scheduled",
                        Notes = "Automatic System Backup (4:30 PM)"
                    };

                    // 4. SAVE TO DB
                    context.BackupLogs.Add(log);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Automatic Backup Recorded in Database Successfully.");

                    // 5. CLEANUP OLD FILES
                    CleanupOldBackups(backupFolder, 30);
                }
                catch (Exception ex)
                {
                    // This will show up in your Visual Studio "Output" window
                    _logger.LogError($"AUTO-BACKUP ERROR: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"INNER ERROR: {ex.InnerException.Message}");
                    }
                }
            }
        }

        // --- NEW METHOD: Auto-Delete old files ---
        private void CleanupOldBackups(string folderPath, int daysToKeep)
        {
            try
            {
                var directory = new DirectoryInfo(folderPath);
                var oldFiles = directory.GetFiles("Auto_HOA_Backup_*.bak")
                                        .Where(f => f.CreationTime < DateTime.Now.AddDays(-daysToKeep))
                                        .ToList();

                foreach (var file in oldFiles)
                {
                    file.Delete();
                    _logger.LogInformation($"Deleted old backup: {file.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to clean up old backups: {ex.Message}");
            }
        }
    }
}