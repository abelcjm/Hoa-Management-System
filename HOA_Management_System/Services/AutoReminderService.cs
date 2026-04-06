using HOA_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace HOA_Management_System.Services
{
    public class AutoReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoReminderService> _logger;

        public AutoReminderService(IServiceProvider serviceProvider, ILogger<AutoReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("--- Auto-Email Service Started ---");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDelinquentAccountsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CRITICAL ERROR inside the background loop.");
                }

                // RESTORED: The robot wakes up once every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task ProcessDelinquentAccountsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HoaDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            _logger.LogInformation("ROBOT WOKE UP: Scanning database for 12-month delinquents...");

            // RESTORED: Only targets people 12 or more months behind
            var severeDelinquents = await context.Properties
                .Include(p => p.Memberships)
                .Include(p => p.MonthlyDues)
                .Where(p => p.MonthlyDues.Count(d => d.Status != "Paid") >= 12)
                .ToListAsync();

            foreach (var property in severeDelinquents)
            {
                // RESTORED: 1. CHECK THE 30-DAY LIMIT USING AUDIT LOGS
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                bool recentlyEmailed = await context.AuditLogs.AnyAsync(a =>
                    a.TableName == "Properties" &&
                    a.RecordId == property.PropertyId &&
                    a.ActionType == "AutoEmail" &&
                    a.Timestamp >= thirtyDaysAgo);

                if (recentlyEmailed)
                {
                    continue; // Skip! We already emailed them this month.
                }

                // 2. GET USER AND EMAIL
                var owner = property.Memberships.FirstOrDefault();
                if (owner == null) continue;

                string? emailAddress = owner.EmailAddress;

                // If it's null, blank, doesn't have an @ symbol, OR is our fake testing domain... skip them!
                if (string.IsNullOrWhiteSpace(emailAddress) ||
                    !emailAddress.Contains("@") ||
                    emailAddress.EndsWith("@dummy-hoa.invalid"))
                {
                    continue;
                }

                // 3. CALCULATE DEBT
                decimal totalOwed = property.MonthlyDues
                    .Where(d => d.Status != "Paid")
                    .Sum(d => d.AmountDue - (d.TotalPaid ?? 0));

                int monthsBehind = property.MonthlyDues.Count(d => d.Status != "Paid");

                // 4. COMPOSE AND SEND EMAIL
                string subject = "URGENT: HOA Account Severely Overdue";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; color: #333;'>
                        <h3>Dear {owner.FirstName} {owner.LastName},</h3>
                        <p>This is an automated notice from the San Isidro Heights HOA.</p>
                        <p>Our records indicate that your account for <strong>Block {property.Block} Lot {property.Lot}</strong> is currently <strong>{monthsBehind} months</strong> in arrears.</p>
                        <p>Your total outstanding balance is <strong style='color:red;'>₱{totalOwed:N2}</strong>.</p>
                        <p>Please log in to your Homeowner Portal to review your statement or contact the HOA administration immediately to settle your account and avoid loss of amenity privileges.</p>
                        <br/>
                        <p>Sincerely,<br/>HOA Board of Directors</p>
                    </div>
                ";

                try
                {
                    await emailService.SendEmailAsync(emailAddress, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"SMTP FAILURE: Google rejected the email. Reason: {ex.Message}");
                    continue; // Stop right here, do not log to the database if email failed
                }

                // 5. LOG IT
                try
                {
                    var auditLog = new AuditLog
                    {
                        UserId = 1, // System Admin ID
                        ActionType = "AutoEmail",
                        TableName = "Properties",
                        RecordId = property.PropertyId,
                        Timestamp = DateTime.Now,
                        Details = $"Sent 12-month delinquency warning to {emailAddress}. Debt: ₱{totalOwed:N2}."
                    };

                    context.AuditLogs.Add(auditLog);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"DATABASE ERROR: Could not save Audit Log. Reason: {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            _logger.LogInformation("ROBOT GOING BACK TO SLEEP.");
        }
    }
}