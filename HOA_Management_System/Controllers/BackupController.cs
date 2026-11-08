using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin")]
    public class BackupController : Controller
    {
        private readonly HoaDbContext _context;
        private readonly IConfiguration _configuration;

        public BackupController(HoaDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string GetBackupFolder()
        {
            var folder = _configuration["BackupSettings:FolderPath"];
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = Path.Combine(AppContext.BaseDirectory, "HOA_Backups");
            }
            return folder;
        }

        public async Task<IActionResult> Index()
        {
            var backups = await _context.BackupLogs
                .Include(b => b.UserAccount)
                .OrderByDescending(b => b.Backup_Date)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            var latest = backups.FirstOrDefault();
            ViewData["LastBackupDate"] = latest?.Backup_Date;

            return View(backups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup()
        {
            // 1. DYNAMICALLY GET DATABASE NAME (Fixes the "HOA_DB does not exist" error)
            string dbName = _context.Database.GetDbConnection().Database;

            // BACKUP FOLDER PATH
            string backupFolder = GetBackupFolder();
            string fileName = $"HOA_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string fullPath = Path.Combine(backupFolder, fileName);

            // Ensure the directory actually exists on the server
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            // --- 2. GET CURRENT USER ID (SESSION FIX) ---
            int currentUserId = 0;
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                currentUserId = int.Parse(userIdString);
            }
            else
            {
                // Fallback: If session is lost, find the first Admin to prevent crash
                var admin = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Role == "Admin");
                currentUserId = admin != null ? admin.UserId : 1;
            }
            // --------------------------------------------

            try
            {
                // 3. EXECUTE SQL BACKUP COMMAND
                string sqlCommand = $"BACKUP DATABASE [{dbName}] TO DISK = '{fullPath}' WITH FORMAT, INIT, NAME = 'Full Backup of {dbName}';";
                await _context.Database.ExecuteSqlRawAsync(sqlCommand);

                // 4. CREATE & SAVE BACKUP LOG
                var backupLog = new BackupLog
                {
                    Backup_File_Name = fileName,
                    Backup_Date = DateTime.Now,
                    Performed_By = currentUserId,
                    Backup_Type = "Manual",
                    Notes = "Backup created successfully via Web Interface"
                };

                _context.BackupLogs.Add(backupLog);
                await _context.SaveChangesAsync();

                // 5. CREATE & SAVE AUDIT LOG
                var auditLog = new AuditLog
                {
                    ActionType = "Create",
                    TableName = "BackupLog",
                    RecordId = backupLog.Backup_ID,
                    UserId = currentUserId,
                    Timestamp = DateTime.Now,
                    Details = $"Manual backup created: {fileName}"
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Database backup created and logged successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Backup failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // 1. DOWNLOAD ACTION
        public async Task<IActionResult> Download(int id)
        {
            var backup = await _context.BackupLogs.FindAsync(id);
            if (backup == null) return NotFound();

            string folder = GetBackupFolder();
            string safeFileName = Path.GetFileName(backup.Backup_File_Name);
            string fullPath = Path.Combine(folder, safeFileName);
            string resolvedPath = Path.GetFullPath(fullPath);
            string resolvedFolder = Path.GetFullPath(folder);

            // Prevent path traversal outside the backup folder
            if (!resolvedPath.StartsWith(resolvedFolder, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(resolvedPath))
            {
                TempData["Error"] = "Physical backup file not found. It may have been deleted.";
                return RedirectToAction("Index");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(resolvedPath);
            return File(fileBytes, "application/octet-stream", safeFileName);
        }

        // 2. RESTORE ACTION (Handle with Care!)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(IFormFile? backupFile, int? backupId)
        {
            // DYNAMICALLY GET DATABASE NAME
            string dbName = _context.Database.GetDbConnection().Database;
            string restoreFilePath = "";

            string backupFolder = GetBackupFolder();

            try
            {
                if (backupFile != null && backupFile.Length > 0)
                {
                    string ext = Path.GetExtension(backupFile.FileName);
                    if (!string.Equals(ext, ".bak", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["Error"] = "Invalid file type. Only .bak files are accepted.";
                        return RedirectToAction("Index");
                    }

                    string tempFolder = Path.Combine(backupFolder, "Temp");
                    if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                    restoreFilePath = Path.Combine(tempFolder, $"Restore_{DateTime.Now.Ticks}.bak");

                    using (var stream = new FileStream(restoreFilePath, FileMode.Create))
                    {
                        await backupFile.CopyToAsync(stream);
                    }
                }
                else if (backupId.HasValue)
                {
                    var existingBackup = await _context.BackupLogs.FindAsync(backupId);
                    if (existingBackup == null) return NotFound();

                    string safeFileName = Path.GetFileName(existingBackup.Backup_File_Name);
                    restoreFilePath = Path.Combine(backupFolder, safeFileName);
                }
                else
                {
                    TempData["Error"] = "No backup file selected.";
                    return RedirectToAction("Index");
                }

                // THE DANGEROUS SQL COMMAND
                string sql = $@"
                USE master;
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [{dbName}] FROM DISK = '{restoreFilePath}' WITH REPLACE;
                ALTER DATABASE [{dbName}] SET MULTI_USER;
                ";

                await _context.Database.ExecuteSqlRawAsync(sql);

                TempData["Success"] = "Database restored successfully! You may need to log in again.";
            }
            catch (Exception ex)
            {
                // Friendlier error for shared hosting limitations
                if (ex.Message.Contains("permission denied") || ex.Message.Contains("USE master"))
                {
                    TempData["Error"] = "Restore Blocked: Shared hosting limits code-based restores for security. Please use the SmarterASP Control Panel to restore backups.";
                }
                else
                {
                    TempData["Error"] = $"Restore Failed: {ex.Message}";
                }
            }

            return RedirectToAction("Index");
        }
    }
}