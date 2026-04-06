using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using HOA_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin", "Treasurer")]
    public class AnalyticsController : Controller
    {
        private readonly HoaDbContext _context;

        public AnalyticsController(HoaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string filterType, int? year, int? period)
        {
            var model = await GenerateAnalyticsModel(filterType, year, period);
            return View(model);
        }

        private async Task<AnalyticsViewModel> GenerateAnalyticsModel(string filterType, int? year, int? period)
        {
            await EnsureDelinquencyHistoryExist();
            await EnsureHistoricalStatsExist();

            var today = DateOnly.FromDateTime(DateTime.Today);

            // --- DEFAULT TO HISTORICAL ---
            string type = string.IsNullOrEmpty(filterType) ? "Historical" : filterType;

            int selectedYear = year ?? today.Year;
            int selectedPeriod = period ?? (type == "Monthly" ? today.Month : (type == "Quarterly" ? (today.Month + 2) / 3 : 0));

            var availableYears = await _context.MonthlyDues.Select(m => m.Year).Distinct().OrderByDescending(y => y).ToListAsync();
            if (!availableYears.Contains(today.Year)) availableYears.Insert(0, today.Year);

            DateOnly filterStart, filterEnd;
            if (type == "Historical")
            {
                filterStart = DateOnly.MinValue;
                filterEnd = DateOnly.MaxValue;
            }
            else if (type == "Monthly")
            {
                filterStart = new DateOnly(selectedYear, selectedPeriod, 1);
                filterEnd = filterStart.AddMonths(1).AddDays(-1);
            }
            else if (type == "Quarterly")
            {
                int startMonth = (selectedPeriod - 1) * 3 + 1;
                filterStart = new DateOnly(selectedYear, startMonth, 1);
                filterEnd = filterStart.AddMonths(3).AddDays(-1);
            }
            else
            {
                filterStart = new DateOnly(selectedYear, 1, 1);
                filterEnd = new DateOnly(selectedYear, 12, 31);
            }

            var filteredTransactions = await _context.Transactions
                .Where(t => t.PaymentDate >= filterStart && t.PaymentDate <= filterEnd).ToListAsync();

            var allBills = await _context.MonthlyDues.ToListAsync();
            var filteredBills = allBills.AsEnumerable();

            if (type != "Historical") filteredBills = filteredBills.Where(b => b.Year == selectedYear);
            if (type == "Quarterly") filteredBills = filteredBills.Where(b => (b.Month - 1) / 3 + 1 == selectedPeriod);
            if (type == "Monthly") filteredBills = filteredBills.Where(b => b.Month == selectedPeriod);

            var filteredBillsList = filteredBills.ToList();

            var strictlyDueBillsList = filteredBillsList
                .Where(b => b.Year < today.Year || (b.Year == today.Year && b.Month <= today.Month))
                .ToList();

            var delinqRecords = await _context.DelinquencyRecords.ToListAsync();
            var collRecords = await _context.CollectionRecords.ToListAsync();

            if (type != "Historical")
            {
                delinqRecords = delinqRecords.Where(r => r.Year == selectedYear).ToList();
                collRecords = collRecords.Where(r => r.Year == selectedYear).ToList();
            }

            var allProperties = await _context.Properties.Include(p => p.MonthlyDues).ToListAsync();
            int homeownerCount = allProperties.Count;

            decimal expectedDuesInPeriod = strictlyDueBillsList.Sum(b => b.AmountDue);
            decimal collectedDuesInPeriod = strictlyDueBillsList.Sum(b => b.TotalPaid ?? 0);
            decimal displayEfficiencyRate = expectedDuesInPeriod > 0 ? (collectedDuesInPeriod / expectedDuesInPeriod) * 100 : 0;
            if (displayEfficiencyRate > 100) displayEfficiencyRate = 100;

            decimal displayDelinquencyRate = 0;
            if (type == "Monthly" && selectedYear == today.Year && selectedPeriod == today.Month)
            {
                int liveDelinqCount = allProperties.Count(p => p.MonthlyDues.Count(d => d.Status != "Paid") >= 36);
                displayDelinquencyRate = homeownerCount > 0 ? ((decimal)liveDelinqCount / homeownerCount) * 100 : 0;
            }
            else if (type == "Monthly")
            {
                displayDelinquencyRate = delinqRecords.FirstOrDefault(r => r.Month == selectedPeriod)?.DelinquencyRate ?? 0;
            }
            else if (type == "Quarterly")
            {
                int startM = (selectedPeriod - 1) * 3 + 1;
                var quarterRecords = delinqRecords.Where(r => r.Month >= startM && r.Month <= startM + 2).ToList();
                displayDelinquencyRate = quarterRecords.Any() ? quarterRecords.Average(r => r.DelinquencyRate) : 0;
            }
            else
            {
                displayDelinquencyRate = delinqRecords.Any() ? delinqRecords.Average(r => r.DelinquencyRate) : 0;
            }

            var rateSetting = await _context.AppSettings.FindAsync("MonthlyFee");
            decimal globalRate = rateSetting != null ? decimal.Parse(rateSetting.Value) : 500;
            decimal monthlyProjected = homeownerCount * globalRate;

            var chartLabels = new List<string>();
            var collectionData = new List<decimal>();
            var targetData = new List<decimal>();

            var delinquencyData = new List<decimal?>();
            var snapshotData = new List<decimal?>();
            var barColors = new List<string>();

            var cumActual = new List<decimal>();
            var cumProj = new List<decimal>();
            decimal runningTotal = 0, runningTarget = 0;

            if (type == "Historical")
            {
                var ascYears = availableYears.OrderBy(y => y).ToList();
                foreach (var y in ascYears)
                {
                    chartLabels.Add(y.ToString());
                    decimal val = filteredTransactions.Where(t => t.PaymentDate.Year == y).Sum(t => t.Amount);
                    collectionData.Add(val);
                    targetData.Add(monthlyProjected * 12);

                    runningTotal += val; cumActual.Add(runningTotal);
                    runningTarget += (monthlyProjected * 12); cumProj.Add(runningTarget);

                    var dr = delinqRecords.Where(r => r.Year == y).Average(r => (decimal?)r.DelinquencyRate) ?? 0;
                    delinquencyData.Add(dr);

                    var frozenEff = collRecords.Where(r => r.Year == y).Average(r => (decimal?)r.EfficiencyRate) ?? 0;
                    snapshotData.Add(frozenEff);

                    barColors.Add("#0f766e");
                }
            }
            else
            {
                var yearTransactions = await _context.Transactions.Where(t => t.PaymentDate.Year == selectedYear).ToListAsync();
                for (int m = 1; m <= 12; m++)
                {
                    chartLabels.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m));
                    DateOnly mStart = new DateOnly(selectedYear, m, 1);
                    DateOnly mEnd = mStart.AddMonths(1).AddDays(-1);

                    decimal val = yearTransactions.Where(t => t.PaymentDate >= mStart && t.PaymentDate <= mEnd).Sum(t => t.Amount);
                    collectionData.Add(val);
                    targetData.Add(monthlyProjected);

                    bool isHighlighted = (type == "Monthly" && m == selectedPeriod) || (type == "Quarterly" && (m - 1) / 3 + 1 == selectedPeriod) || (type == "Yearly");
                    barColors.Add(isHighlighted ? "#0f766e" : "#e2e8f0");

                    bool isFuture = selectedYear > today.Year || (selectedYear == today.Year && m > today.Month);
                    bool isCurrentMonth = selectedYear == today.Year && m == today.Month;

                    if (isFuture)
                    {
                        delinquencyData.Add(null);
                        snapshotData.Add(null);
                        cumActual.Add(runningTotal);
                    }
                    else if (isCurrentMonth)
                    {
                        int liveDelinqCount = allProperties.Count(p => p.MonthlyDues.Count(d => d.Status != "Paid") >= 36);
                        delinquencyData.Add(homeownerCount > 0 ? ((decimal)liveDelinqCount / homeownerCount) * 100 : 0);

                        var mBills = allBills.Where(b => b.Year == selectedYear && b.Month == m).ToList();
                        decimal tgt = mBills.Sum(b => b.AmountDue);
                        decimal col = mBills.Sum(b => b.TotalPaid ?? 0);
                        decimal curEff = tgt > 0 ? (col / tgt) * 100 : 0;
                        if (curEff > 100) curEff = 100;
                        snapshotData.Add(curEff);

                        runningTotal += val; cumActual.Add(runningTotal);
                    }
                    else
                    {
                        delinquencyData.Add(delinqRecords.FirstOrDefault(x => x.Month == m)?.DelinquencyRate ?? 0);
                        snapshotData.Add(collRecords.FirstOrDefault(x => x.Month == m)?.EfficiencyRate ?? 0);

                        runningTotal += val; cumActual.Add(runningTotal);
                    }

                    runningTarget += monthlyProjected; cumProj.Add(runningTarget);
                }
            }

            int countPaid = 0, countPartial = 0, countUnpaid = 0;
            foreach (var prop in allProperties)
            {
                var propBills = strictlyDueBillsList.Where(b => b.PropertyId == prop.PropertyId).ToList();
                if (!propBills.Any()) continue;

                int total = propBills.Count;
                int paid = propBills.Count(b => b.Status == "Paid");
                int unpaid = propBills.Count(b => b.Status == "Unpaid" && (b.TotalPaid == null || b.TotalPaid == 0));

                if (paid == total) countPaid++;
                else if (unpaid == total) countUnpaid++;
                else countPartial++;
            }

            var blockHeatmap = new List<AnalyticsViewModel.BlockHeatmapDto>();
            if (type == "Historical")
            {
                var groupedByBlock = allProperties.GroupBy(p => p.Block);
                foreach (var blockGroup in groupedByBlock)
                {
                    int strictDelinq = blockGroup.Count(p => p.MonthlyDues.Count(d => d.Status != "Paid") >= 36);
                    decimal debt = blockGroup.Sum(p => strictlyDueBillsList.Where(b => b.PropertyId == p.PropertyId).Sum(d => d.AmountDue - (d.TotalPaid ?? 0)));
                    decimal rate = blockGroup.Any() ? ((decimal)strictDelinq / blockGroup.Count()) * 100 : 0;

                    blockHeatmap.Add(new AnalyticsViewModel.BlockHeatmapDto
                    {
                        Block = blockGroup.Key ?? "Unknown",
                        PropertyCount = blockGroup.Count(),
                        DelinquentCount = strictDelinq,
                        DelinquencyRate = rate,
                        TotalDebt = debt
                    });
                }
                blockHeatmap = blockHeatmap.OrderByDescending(b => b.DelinquencyRate).ToList();
            }

            // --- TOP 5 ARREARS (ONLY RUNS ON HISTORICAL) ---
            var topList = new List<OverdueAccountDto>();
            if (type == "Historical")
            {
                var stats = allBills.Where(d => d.Status != "Paid")
                    .GroupBy(d => d.PropertyId)
                    .Select(g => new { Id = g.Key, Count = g.Count(), Debt = g.Sum(d => d.AmountDue - (d.TotalPaid ?? 0)) })
                    .OrderByDescending(x => x.Debt).Take(5).ToList();

                foreach (var s in stats)
                {
                    var prop = allProperties.FirstOrDefault(p => p.PropertyId == s.Id);
                    var member = await _context.Memberships.FirstOrDefaultAsync(m => m.PropertyId == s.Id);
                    if (prop != null) topList.Add(new OverdueAccountDto
                    {
                        PropertyId = s.Id,
                        Homeowner = member != null ? $"{member.LastName}, {member.FirstName}" : "Unknown",
                        BlockLot = $"B{prop.Block}-L{prop.Lot}",
                        MonthsOverdue = s.Count,
                        TotalDebt = s.Debt
                    });
                }
            }

            // --- MEATY INSIGHTS GENERATION (STANDALONE & CONCEPTUAL) ---
            var insights = new List<string>();
            decimal totalCollected = filteredTransactions.Sum(t => t.Amount);

            // Outstanding logic strictly prevents negative debt from overpayments
            decimal totalOutstanding = strictlyDueBillsList
                .Where(b => b.AmountDue > (b.TotalPaid ?? 0))
                .Sum(b => b.AmountDue - (b.TotalPaid ?? 0));

            // 1. Cash Flow vs Target
            insights.Add($"<strong>Financial Overview:</strong> The HOA has collected a total of <strong>₱{totalCollected:N0}</strong> in cash deposits across all income sources. Compared to the actual billed target of <strong>₱{expectedDuesInPeriod:N0}</strong> for regular dues, this metric helps visualize the gap between expected monthly revenue and actual cash on hand.");

            // 2. Compliance Meaning
            if (displayEfficiencyRate >= 85)
                insights.Add($"<strong>Excellent Compliance:</strong> Billing compliance is exceptionally strong at <span class='text-success fw-bold'>{displayEfficiencyRate:N2}%</span>. This indicates that the vast majority of homeowners are consistently paying their assessed dues on time, ensuring stable funding for HOA operations.");
            else if (displayEfficiencyRate >= 60)
                insights.Add($"<strong>Stable Compliance:</strong> Billing compliance is currently at <span class='text-warning fw-bold'>{displayEfficiencyRate:N2}%</span>. While a significant portion of bills are being settled, there is noticeable friction in collections that the administration should monitor to prevent future cash flow dips.");
            else if (expectedDuesInPeriod > 0)
                insights.Add($"<strong>Action Needed:</strong> Billing compliance has dropped to a critical low of <span class='text-danger fw-bold'>{displayEfficiencyRate:N2}%</span>. A large portion of assessed revenue remains uncollected, which may soon disrupt the HOA's ability to fund essential maintenance and community services.");

            // 3. Outstanding Debt
            if (totalOutstanding > 0)
                insights.Add($"<strong>Outstanding Balances:</strong> There is an active shortfall of <span class='text-danger fw-bold'>₱{totalOutstanding:N0}</span> in unpaid dues. This represents pure, realized debt from valid billing cycles that requires immediate and targeted collection efforts.");

            // 4. Household Behavior
            int totalProps = countPaid + countPartial + countUnpaid;
            if (totalProps > 0)
            {
                decimal paidPct = ((decimal)countPaid / totalProps) * 100;
                insights.Add($"<strong>Household Payment Behavior:</strong> Among the active accounts evaluated, <span class='text-success fw-bold'>{paidPct:N0}%</span> of households have completely cleared their balances. The remaining fraction represents homes that are either partially paid or entirely in arrears, signaling a need for focused payment reminders.");
            }

            // 5. Delinquency Impact
            if (displayDelinquencyRate > 15)
                insights.Add($"<strong>Severe Delinquency Warning:</strong> The community's severe delinquency rate (households missing 36+ months of payments) has reached a concerning <span class='text-danger fw-bold'>{displayDelinquencyRate:N2}%</span>. Prolonged non-payment at this scale heavily impacts long-term reserves. {(type == "Historical" ? "The administration should review the highest arrears to prioritize accounts for formal legal notices or restriction protocols." : "Switching to the 'Historical' filter will reveal the specific top debtors requiring intervention.")}");
            else
                insights.Add($"<strong>Long-Term Debt Health:</strong> The severe delinquency rate (3+ years unpaid) is currently averaging <span class='text-dark fw-bold'>{displayDelinquencyRate:N2}%</span>. Keeping this metric strictly managed ensures that the community does not carry toxic, unrecoverable debt across multiple fiscal years.");
            
            return new AnalyticsViewModel
            {
                FilterType = type,
                SelectedYear = selectedYear,
                SelectedPeriod = selectedPeriod,
                AvailableYears = availableYears,
                TotalCollected = totalCollected,
                TotalOutstanding = totalOutstanding,
                TotalExpectedDues = expectedDuesInPeriod,
                AverageEfficiency = displayEfficiencyRate,
                AverageDelinquencyRate = displayDelinquencyRate,
                ChartLabels = chartLabels,
                CollectionData = collectionData,
                TargetData = targetData,
                DelinquencyData = delinquencyData,
                SnapshotLabels = chartLabels,
                SnapshotData = snapshotData,
                BarColors = barColors,
                CountPaid = countPaid,
                CountPartial = countPartial,
                CountUnpaid = countUnpaid,
                CumulativeActual = cumActual,
                CumulativeProjected = cumProj,
                BlockHeatmap = blockHeatmap,
                AmountDues = filteredTransactions.Where(t => t.PaymentType == "MonthlyDue" || t.PaymentType == "Monthly Dues").Sum(t => t.Amount),
                AmountCertificates = filteredTransactions.Where(t => t.PaymentType == "Certificate" || t.PaymentType == "Certification").Sum(t => t.Amount),
                AmountStickers = filteredTransactions.Where(t => t.PaymentType.Contains("Sticker")).Sum(t => t.Amount),
                TopDelinquents = topList,
                GeneratedInsights = insights
            };
        }

        private async Task EnsureDelinquencyHistoryExist()
        {
            var today = DateTime.Today;
            var earliestBill = await _context.MonthlyDues.OrderBy(m => m.Year).ThenBy(m => m.Month).FirstOrDefaultAsync();
            if (earliestBill == null) return;

            int totalProps = await _context.Properties.CountAsync();
            if (totalProps == 0) return;

            var currentDate = new DateTime(earliestBill.Year, earliestBill.Month, 1);
            var limitDate = new DateTime(today.Year, today.Month, 1);

            while (currentDate < limitDate)
            {
                bool exists = await _context.DelinquencyRecords.AnyAsync(d => d.Year == currentDate.Year && d.Month == currentDate.Month);
                if (!exists)
                {
                    var year = currentDate.Year;
                    var month = currentDate.Month;
                    var delinquentCount = await _context.Properties
                        .CountAsync(p => p.MonthlyDues.Count(d => (d.Year < year || (d.Year == year && d.Month <= month)) && d.Status != "Paid") >= 36);

                    decimal rate = ((decimal)delinquentCount / totalProps) * 100;
                    _context.DelinquencyRecords.Add(new DelinquencyRecord
                    {
                        Month = month,
                        Year = year,
                        DelinquencyRate = rate,
                        TotalHomeowners = totalProps,
                        DelinquentCount = delinquentCount,
                        IsFinalized = true
                    });
                }
                currentDate = currentDate.AddMonths(1);
            }
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