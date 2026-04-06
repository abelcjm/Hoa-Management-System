using HOA_Management_System.Models;
using HOA_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HOA_Management_System.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HOA_Management_System.Controllers
{
    public class DashboardController : Controller
    {
        private readonly HoaDbContext _context;

        public DashboardController(HoaDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // ADMIN DASHBOARD
        // =========================================================
        [UserAuth("Admin", "Treasurer")]
        public async Task<IActionResult> Index()
        {
            await EnsureMonthlyBillsExist();
            await EnsureHistoricalStatsExist();

            var today = DateTime.Today;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);

            int totalHomeowners = await _context.Properties.CountAsync();

            var currentMonthBills = await _context.MonthlyDues
                .Where(m => m.Month == today.Month && m.Year == today.Year)
                .ToListAsync();

            decimal totalOutstanding = currentMonthBills
                .Where(m => m.AmountDue > (m.TotalPaid ?? 0))
                .Sum(m => m.AmountDue - (m.TotalPaid ?? 0));

            decimal targetThisMonth = currentMonthBills.Sum(m => m.AmountDue);
            decimal collectedThisMonth = currentMonthBills.Sum(m => m.TotalPaid ?? 0);

            decimal efficiency = targetThisMonth > 0 ? (collectedThisMonth / targetThisMonth) * 100 : 0;
            if (efficiency > 100) efficiency = 100;

            var lastMonthRecord = await _context.CollectionRecords
                .FirstOrDefaultAsync(r => r.Month == lastMonthStart.Month && r.Year == lastMonthStart.Year);

            decimal lastMonthEfficiency = lastMonthRecord?.EfficiencyRate ?? 0;
            decimal efficiencyChange = efficiency - lastMonthEfficiency;

            int paidCount = currentMonthBills.Count(b => b.Status == "Paid");
            int partialCount = currentMonthBills.Count(b => b.Status == "Partial");
            int unpaidCount = currentMonthBills.Count(b => b.Status == "Unpaid");

            var thresholdSetting = await _context.AppSettings.FindAsync("HealthThreshold");
            decimal healthyThreshold = thresholdSetting != null ? decimal.Parse(thresholdSetting.Value) : 90;

            string healthStatus = "Critical";
            string insight = "";
            string suggestion = "";

            string trendText = "";
            if (efficiencyChange > 0) trendText = $"improved by <span class='text-success fw-bold'>{efficiencyChange:N1}%</span>";
            else if (efficiencyChange < 0) trendText = $"declined by <span class='text-danger fw-bold'>{Math.Abs(efficiencyChange):N1}%</span>";
            else trendText = "remained stable";

            bool isLateInMonth = today.Day > 20;

            if (targetThisMonth == 0)
            {
                healthStatus = "Pending";
                insight = "Billing records for this month have not been generated yet.";
            }
            else if (efficiency >= healthyThreshold)
            {
                healthStatus = "Healthy";
                insight = $"<strong>Excellent.</strong> Billing compliance is {efficiency:N0}%, which has {trendText} compared to last month. <br/>" +
                          "Suggestion: Maintain current operations.";
            }
            else
            {
                if (efficiency >= 75) healthStatus = "Average";
                else if (efficiency >= 50) healthStatus = "Warning";
                else healthStatus = "Critical";

                insight = $"Current billing compliance is {efficiency:N0}%, which has {trendText} compared to last month. ";

                if (isLateInMonth)
                    suggestion = $"<strong>Action Needed:</strong> It is past the 20th. Please contact the {unpaidCount} homeowners with outstanding balances for this month.";
                else
                    suggestion = "<strong>Suggestion:</strong> It is early in the month. Continue monitoring incoming payments.";

                insight += "<br/>" + suggestion;
            }

            var trendLabels = new List<string>();
            var trendCollected = new List<decimal>();
            var trendTarget = new List<decimal>();

            var historyStats = await _context.CollectionRecords
                .Where(r => r.Year >= today.Year - 1)
                .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
                .Take(5)
                .ToListAsync();

            historyStats = historyStats.OrderBy(r => r.Year).ThenBy(r => r.Month).ToList();

            foreach (var stat in historyStats)
            {
                trendLabels.Add(new DateTime(stat.Year, stat.Month, 1).ToString("MMM yyyy"));
                trendCollected.Add(stat.TotalCollected);
                trendTarget.Add(stat.TotalTarget);
            }

            trendLabels.Add(today.ToString("MMM yyyy"));
            trendCollected.Add(collectedThisMonth);
            trendTarget.Add(targetThisMonth);

            var model = new AdminDashboardViewModel
            {
                TotalHomeowners = totalHomeowners,
                TotalCollectedThisMonth = collectedThisMonth,
                TotalOutstandingBalance = totalOutstanding,
                CollectionEfficiency = efficiency,
                EfficiencyChange = efficiencyChange,
                HealthStatus = healthStatus,
                InsightMessage = insight,
                TrendLabels = trendLabels,
                TrendCollected = trendCollected,
                TrendTarget = trendTarget,
                PiePaid = paidCount,
                PiePartial = partialCount,
                PieUnpaid = unpaidCount
            };

            return View(model);
        }

        // =========================================================
        // ENCODER DASHBOARD
        // =========================================================
        [UserAuth("Encoder")]
        public async Task<IActionResult> EncoderIndex()
        {
            await EnsureMonthlyBillsExist();
            await EnsureHistoricalStatsExist();

            var today = DateTime.Today;
            var todayDateOnly = DateOnly.FromDateTime(today);

            // CARDS DATA
            var currentMonthTransactions = await _context.Transactions
                .Where(t => t.PaymentDate.Month == today.Month && t.PaymentDate.Year == today.Year)
                .ToListAsync();

            decimal totalCollected = currentMonthTransactions.Sum(t => t.Amount);

            // Collected Today
            decimal collectedToday = currentMonthTransactions
                .Where(t => t.PaymentDate == todayDateOnly)
                .Sum(t => t.Amount);

            // NEGATIVE DEBT FIX: Only count properties that actually owe money
            var overdueProperties = await _context.MonthlyDues
                .Where(d => d.AmountDue > (d.TotalPaid ?? 0) &&
                            (d.Year < today.Year || (d.Year == today.Year && d.Month < today.Month)))
                .Select(d => d.PropertyId)
                .Distinct()
                .CountAsync();

            var currentMonthBills = await _context.MonthlyDues
                .Where(m => m.Month == today.Month && m.Year == today.Year)
                .ToListAsync();

            // Outstanding Balance + Unpaid Accounts Count
            decimal totalOutstanding = currentMonthBills
                .Where(m => m.AmountDue > (m.TotalPaid ?? 0))
                .Sum(m => m.AmountDue - (m.TotalPaid ?? 0));

            int unpaidAccountsThisMonth = currentMonthBills
                .Count(m => m.AmountDue > (m.TotalPaid ?? 0));

            // RECENT TRANSACTIONS
            var recentTrans = await _context.Transactions
                .Include(t => t.Property).ThenInclude(p => p.Memberships)
                .OrderByDescending(t => t.TransactionId)
                .Take(13)
                .ToListAsync();

            var transList = recentTrans.Select(t => new RecentTransactionDto
            {
                Date = t.PaymentDate.ToString("yyyy-MM-dd"),
                Homeowner = t.Property.Memberships.FirstOrDefault()?.LastName ?? "Unknown",
                Type = t.PaymentType,
                Amount = t.Amount,
                OrNumber = t.OrNumber
            }).ToList();

            // --- COLLECTION HIT LIST (All Overdue Accounts, No Limit) ---
            var overdueRaw = await _context.MonthlyDues
                .Include(d => d.Property)
                .Where(d => d.AmountDue > (d.TotalPaid ?? 0) &&
                            (d.Year < today.Year || (d.Year == today.Year && d.Month < today.Month)))
                .ToListAsync();

            var overdueList = overdueRaw
                .GroupBy(d => d.Property)
                .Select(g => new OverdueAccountDto
                {
                    PropertyId = g.Key.PropertyId,
                    Homeowner = "", // Hidden for operational privacy
                    BlockLot = $"B{g.Key.Block:00}-L{g.Key.Lot:00}",
                    MonthsOverdue = g.Count(),
                    TotalDebt = g.Sum(d => d.AmountDue - (d.TotalPaid ?? 0))
                })
                .OrderByDescending(o => o.TotalDebt)
                .ToList(); 

            // DAILY TREND
            var trendLabels = new List<string>();
            var trendValues = new List<decimal>();

            for (int i = 7; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var targetDateOnly = DateOnly.FromDateTime(date);

                decimal dailySum = await _context.Transactions
                    .Where(t => t.PaymentDate == targetDateOnly)
                    .SumAsync(t => t.Amount);

                trendLabels.Add(date.ToString("MMM dd"));
                trendValues.Add(dailySum);
            }

            var model = new EncoderDashboardViewModel
            {
                TotalCollectedThisMonth = totalCollected,
                CollectedToday = collectedToday,
                TotalOutstandingBalance = totalOutstanding,
                UnpaidAccountsThisMonthCount = unpaidAccountsThisMonth,
                OverdueAccountsCount = overdueProperties,
                RecentTransactions = transList,
                TopOverdueAccounts = overdueList,
                DailyTrendLabels = trendLabels,
                DailyTrendValues = trendValues
            };

            return View(model);
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private async Task EnsureMonthlyBillsExist()
        {
            var today = DateTime.Today;
            int currentMonth = today.Month;
            int currentYear = today.Year;

            var propertiesWithoutBill = await _context.Properties
                .Where(p => !p.MonthlyDues.Any(d => d.Month == currentMonth && d.Year == currentYear))
                .ToListAsync();

            if (!propertiesWithoutBill.Any()) return;

            var rateSetting = await _context.AppSettings.FindAsync("MonthlyFee");
            decimal globalRate = rateSetting != null ? decimal.Parse(rateSetting.Value) : 500;

            var newBills = new List<MonthlyDue>();
            foreach (var prop in propertiesWithoutBill)
            {
                newBills.Add(new MonthlyDue
                {
                    PropertyId = prop.PropertyId,
                    Month = currentMonth,
                    Year = currentYear,
                    AmountDue = globalRate,
                    TotalPaid = 0,
                    Status = "Unpaid"
                });
            }
            _context.MonthlyDues.AddRange(newBills);
            await _context.SaveChangesAsync();
        }

        private async Task EnsureHistoricalStatsExist()
        {
            var today = DateTime.Today;
            var earliestBill = await _context.MonthlyDues.OrderBy(m => m.Year).ThenBy(m => m.Month).FirstOrDefaultAsync();
            if (earliestBill == null) return;

            var currentDate = new DateTime(earliestBill.Year, earliestBill.Month, 1);
            var limitDate = new DateTime(today.Year, today.Month, 1);

            while (currentDate < limitDate)
            {
                bool exists = await _context.CollectionRecords
                    .AnyAsync(r => r.Month == currentDate.Month && r.Year == currentDate.Year);

                if (!exists)
                {
                    var year = currentDate.Year;
                    var month = currentDate.Month;

                    decimal target = await _context.MonthlyDues
                        .Where(d => d.Month == month && d.Year == year)
                        .SumAsync(d => d.AmountDue);

                    decimal collected = await _context.MonthlyDues
                        .Where(d => d.Month == month && d.Year == year && d.DateFullyPaid != null &&
                                   (d.DateFullyPaid.Value.Year < year ||
                                   (d.DateFullyPaid.Value.Year == year && d.DateFullyPaid.Value.Month <= month)))
                        .SumAsync(d => d.TotalPaid ?? 0);

                    decimal efficiency = target > 0 ? (collected / target) * 100 : 0;
                    if (efficiency > 100) efficiency = 100;

                    _context.CollectionRecords.Add(new CollectionRecord
                    {
                        Month = month,
                        Year = year,
                        TotalTarget = target,
                        TotalCollected = collected,
                        EfficiencyRate = efficiency,
                        IsFinalized = true
                    });
                }
                currentDate = currentDate.AddMonths(1);
            }
            await _context.SaveChangesAsync();
        }
    }
}