using ClosedXML.Excel;
using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using HOA_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin", "Treasurer")]
    public class ReportsController : Controller
    {
        private readonly HoaDbContext _context;

        public ReportsController(HoaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var years = await _context.MonthlyDues.Select(m => m.Year).Distinct().OrderByDescending(y => y).ToListAsync();
            if (!years.Contains(DateTime.Now.Year)) years.Insert(0, DateTime.Now.Year);

            var model = new ReportsViewModel
            {
                AvailableYears = years,
                SelectedYear = DateTime.Now.Year,
                FilterType = "Historical" // Default to Historical
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(ReportsViewModel model)
        {
            if (model.ReportType == "Financial") return await ExportFinancialReport(model);
            else if (model.ReportType == "DHSUD") return await ExportDhsudReport(model.ExportFormat);
            return RedirectToAction("Index");
        }

        // ==========================================
        // 1. FINANCIAL REPORT
        // ==========================================
        private async Task<IActionResult> ExportFinancialReport(ReportsViewModel input)
        {
            int year = input.SelectedYear;
            string type = input.FilterType ?? "Historical";
            int period = input.SelectedPeriod ?? 0;
            var today = DateOnly.FromDateTime(DateTime.Today);

            var availableYears = await _context.MonthlyDues.Select(m => m.Year).Distinct().OrderByDescending(y => y).ToListAsync();
            if (!availableYears.Contains(today.Year)) availableYears.Insert(0, today.Year);

            // Filter Logic
            DateOnly filterStart, filterEnd;
            if (type == "Historical")
            {
                filterStart = DateOnly.MinValue;
                filterEnd = DateOnly.MaxValue;
            }
            else if (type == "Monthly")
            {
                filterStart = new DateOnly(year, period, 1);
                filterEnd = filterStart.AddMonths(1).AddDays(-1);
            }
            else if (type == "Quarterly")
            {
                int startMonth = (period - 1) * 3 + 1;
                filterStart = new DateOnly(year, startMonth, 1);
                filterEnd = filterStart.AddMonths(3).AddDays(-1);
            }
            else
            {
                filterStart = new DateOnly(year, 1, 1);
                filterEnd = new DateOnly(year, 12, 31);
            }

            var filteredTransactions = await _context.Transactions
                .Where(t => t.PaymentDate >= filterStart && t.PaymentDate <= filterEnd)
                .OrderBy(t => t.PaymentDate).ToListAsync();

            var allBills = await _context.MonthlyDues.ToListAsync();
            var filteredBills = allBills.AsEnumerable();

            if (type != "Historical") filteredBills = filteredBills.Where(b => b.Year == year);
            if (type == "Quarterly") filteredBills = filteredBills.Where(b => (b.Month - 1) / 3 + 1 == period);
            if (type == "Monthly") filteredBills = filteredBills.Where(b => b.Month == period);

            var filteredBillsList = filteredBills.ToList();

            // TIME WALL FIX
            var strictlyDueBillsList = filteredBillsList
                .Where(b => b.Year < today.Year || (b.Year == today.Year && b.Month <= today.Month))
                .ToList();

            var allProps = await _context.Properties.Include(p => p.MonthlyDues).ToListAsync();
            int homeownerCount = allProps.Count;

            var delinqRecords = await _context.DelinquencyRecords.ToListAsync();
            var collRecords = await _context.CollectionRecords.ToListAsync();

            if (type != "Historical")
            {
                delinqRecords = delinqRecords.Where(r => r.Year == year).ToList();
                collRecords = collRecords.Where(r => r.Year == year).ToList();
            }

            var rateSetting = await _context.AppSettings.FindAsync("MonthlyFee");
            decimal globalRate = rateSetting != null ? decimal.Parse(rateSetting.Value) : 500;
            decimal monthlyProjected = homeownerCount * globalRate;

            // Efficiency & Delinquency Logic
            decimal expectedDuesInPeriod = strictlyDueBillsList.Sum(b => b.AmountDue);
            decimal collectedDuesInPeriod = strictlyDueBillsList.Sum(b => b.TotalPaid ?? 0);
            decimal displayEfficiencyRate = expectedDuesInPeriod > 0 ? (collectedDuesInPeriod / expectedDuesInPeriod) * 100 : 0;
            if (displayEfficiencyRate > 100) displayEfficiencyRate = 100;

            decimal displayDelinquencyRate = 0;
            if (type == "Monthly" && year == today.Year && period == today.Month)
            {
                int liveDelinqCount = allProps.Count(p => p.MonthlyDues.Count(d => d.Status != "Paid") >= 36);
                displayDelinquencyRate = homeownerCount > 0 ? ((decimal)liveDelinqCount / homeownerCount) * 100 : 0;
            }
            else if (type == "Monthly") displayDelinquencyRate = delinqRecords.FirstOrDefault(r => r.Month == period)?.DelinquencyRate ?? 0;
            else if (type == "Quarterly") displayDelinquencyRate = delinqRecords.Any() ? delinqRecords.Average(r => r.DelinquencyRate) : 0;
            else displayDelinquencyRate = delinqRecords.Any() ? delinqRecords.Average(r => r.DelinquencyRate) : 0;

            // Build Charts & Metrics
            var chartLabels = new List<string>();
            var collectionData = new List<decimal>();
            var targetData = new List<decimal>();
            var delinquencyData = new List<decimal?>();
            var snapshotData = new List<decimal?>();
            var barColors = new List<string>();

            decimal runningTotal = 0, runningTarget = 0;
            var cumActual = new List<decimal>();
            var cumProj = new List<decimal>();

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

                    delinquencyData.Add(delinqRecords.Where(r => r.Year == y).Average(r => (decimal?)r.DelinquencyRate) ?? 0);
                    snapshotData.Add(collRecords.Where(r => r.Year == y).Average(r => (decimal?)r.EfficiencyRate) ?? 0);
                    barColors.Add("#0f766e");
                }
            }
            else
            {
                var yearTransactions = await _context.Transactions.Where(t => t.PaymentDate.Year == year).ToListAsync();
                for (int m = 1; m <= 12; m++)
                {
                    chartLabels.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m));
                    DateOnly mStart = new DateOnly(year, m, 1);
                    DateOnly mEnd = mStart.AddMonths(1).AddDays(-1);

                    decimal val = yearTransactions.Where(t => t.PaymentDate >= mStart && t.PaymentDate <= mEnd).Sum(t => t.Amount);
                    collectionData.Add(val);
                    targetData.Add(monthlyProjected);

                    bool isHighlighted = (type == "Monthly" && m == period) || (type == "Quarterly" && (m - 1) / 3 + 1 == period) || (type == "Yearly");
                    barColors.Add(isHighlighted ? "#0f766e" : "#e2e8f0");

                    bool isFuture = year > today.Year || (year == today.Year && m > today.Month);
                    bool isCurrentMonth = year == today.Year && m == today.Month;

                    if (isFuture)
                    {
                        delinquencyData.Add(null);
                        snapshotData.Add(null);
                        cumActual.Add(runningTotal);
                    }
                    else if (isCurrentMonth)
                    {
                        int liveDelinqCount = allProps.Count(p => p.MonthlyDues.Count(d => d.Status != "Paid") >= 36);
                        delinquencyData.Add(homeownerCount > 0 ? ((decimal)liveDelinqCount / homeownerCount) * 100 : 0);

                        var mBills = allBills.Where(b => b.Year == year && b.Month == m).ToList();
                        decimal tgt = mBills.Sum(b => b.AmountDue);
                        decimal col = mBills.Sum(b => b.TotalPaid ?? 0);
                        decimal curEff = tgt > 0 ? (col / tgt) * 100 : 0;
                        snapshotData.Add(curEff > 100 ? 100 : curEff);

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

            // Top Delinquents (Negative Debt Fix)
            var stats = allBills.Where(d => d.Status != "Paid")
                .GroupBy(d => d.PropertyId)
                .Select(g => new { Id = g.Key, Count = g.Count(), Debt = g.Where(d => d.AmountDue > (d.TotalPaid ?? 0)).Sum(d => d.AmountDue - (d.TotalPaid ?? 0)) })
                .OrderByDescending(x => x.Debt).Take(10).ToList();

            var topList = new List<OverdueAccountDto>();
            foreach (var s in stats)
            {
                var prop = allProps.FirstOrDefault(p => p.PropertyId == s.Id);
                var member = await _context.Memberships.FirstOrDefaultAsync(m => m.PropertyId == s.Id);
                if (prop != null) topList.Add(new OverdueAccountDto
                {
                    Homeowner = member != null ? $"{member.LastName}, {member.FirstName}" : "Unknown",
                    BlockLot = $"B{prop.Block}-L{prop.Lot}",
                    MonthsOverdue = s.Count,
                    TotalDebt = s.Debt
                });
            }

            // Outstanding Calculations
            decimal totalOutstanding = strictlyDueBillsList
                .Where(b => b.AmountDue > (b.TotalPaid ?? 0))
                .Sum(b => b.AmountDue - (b.TotalPaid ?? 0));

            // --- DETAILED SECTIONAL INSIGHTS GENERATION ---
            decimal totalCollected = filteredTransactions.Sum(t => t.Amount);
            decimal amountDues = filteredTransactions.Where(t => t.PaymentType == "MonthlyDue" || t.PaymentType == "Monthly Dues").Sum(t => t.Amount);
            decimal amountCerts = filteredTransactions.Where(t => t.PaymentType == "Certificate").Sum(t => t.Amount);
            decimal amountStickers = filteredTransactions.Where(t => t.PaymentType.Contains("Sticker")).Sum(t => t.Amount);

            // 1. EXECUTIVE INSIGHT
            string execInsight = $"<strong>Executive Summary:</strong> For the <strong>{type}</strong> period of {year}, the HOA generated a strict billing target of <strong>₱{expectedDuesInPeriod:N0}</strong>. Against this target, <strong>₱{totalCollected:N0}</strong> was successfully collected across all revenue streams, resulting in an overall collection efficiency of <strong>{displayEfficiencyRate:N2}%</strong>. Currently, there is an active outstanding deficit of <strong>₱{totalOutstanding:N0}</strong> sitting in arrears from uncollected bills. The community's severe delinquency metric (accounts 36+ months behind) currently averages <strong>{displayDelinquencyRate:N2}%</strong>.";

            // 2. TRENDS INSIGHT
            var validCollections = collectionData.Where(c => c > 0).ToList();
            decimal maxColl = validCollections.Any() ? validCollections.Max() : 0;
            decimal minColl = validCollections.Any() ? validCollections.Min() : 0;
            string maxMonth = chartLabels.ElementAtOrDefault(collectionData.IndexOf(maxColl)) ?? "the peak month";
            string minMonth = chartLabels.ElementAtOrDefault(collectionData.IndexOf(minColl)) ?? "the lowest month";

            decimal finalActual = cumActual.LastOrDefault();
            decimal finalProj = cumProj.LastOrDefault();
            decimal deficit = finalProj - finalActual;
            string deficitText = deficit > 0 ? $"a shortfall of <strong>₱{deficit:N0}</strong>" : $"a surplus of <strong>₱{Math.Abs(deficit):N0}</strong>";

            string trendInsight = $"<strong>Financial Trends Analysis:</strong> As shown in the 'Collection vs Target' graph, <strong>{maxMonth}</strong> was the strongest performing month, bringing in <strong>₱{maxColl:N0}</strong>. Conversely, the lowest active collection period was <strong>{minMonth}</strong> at <strong>₱{minColl:N0}</strong>. Looking at the 'Projected vs Actual' cumulative line chart, the HOA aimed for a linear fiscal target of <strong>₱{finalProj:N0}</strong> by the end of the evaluated period. The actual realized cash flow reached <strong>₱{finalActual:N0}</strong>, representing {deficitText} compared to the ideal financial projection.";

            // 3. REVENUE & COMPLIANCE INSIGHT
            decimal totalRevForPie = amountDues + amountCerts + amountStickers;
            decimal duesPct = totalRevForPie > 0 ? (amountDues / totalRevForPie) * 100 : 0;
            decimal certsPct = totalRevForPie > 0 ? (amountCerts / totalRevForPie) * 100 : 0;
            decimal stickPct = totalRevForPie > 0 ? (amountStickers / totalRevForPie) * 100 : 0;

            decimal startComp = snapshotData.FirstOrDefault(x => x.HasValue) ?? 0m;
            decimal endComp = snapshotData.LastOrDefault(x => x.HasValue) ?? 0m;
            decimal peakDelinq = delinquencyData.Any(x => x.HasValue) ? delinquencyData.Where(x => x.HasValue).Max(x => x.Value) : 0m;

            string revInsight = $"<strong>Compliance & Distribution:</strong> The 'Revenue Sources' pie chart illustrates that income is distributed across Regular Dues (<strong>{duesPct:N1}%</strong>), Certificates (<strong>{certsPct:N1}%</strong>), and Stickers (<strong>{stickPct:N1}%</strong>). The 'Historical Compliance' line tracks month-end payment behaviors, showing on-time compliance shifted from <strong>{startComp:N1}%</strong> early in the period to <strong>{endComp:N1}%</strong> recently. The 'Delinquency Rate Trend' graph highlights that the percentage of severely overdue properties peaked at <strong>{peakDelinq:N1}%</strong> during this timeframe.";

            // 4. DETAILS INSIGHT
            decimal top10Debt = topList.Sum(t => t.TotalDebt);
            string detailInsight = $"<strong>Arrears Concentration:</strong> The table above isolates the top {(topList.Count < 10 ? topList.Count : 10)} worst-offending accounts. Combined, these specific households hold a massive <strong>₱{top10Debt:N0}</strong> in unpaid dues. The single highest delinquent account holds a balance of <strong>₱{(topList.FirstOrDefault()?.TotalDebt ?? 0):N0}</strong>, spanning <strong>{(topList.FirstOrDefault()?.MonthsOverdue ?? 0)} months</strong>. Immediate intervention, such as restricted amenity access and formal demand letters, is heavily recommended for this concentrated group to prevent further financial liability.";

            var reportModel = new ReportsViewModel
            {
                SelectedYear = year,
                FilterType = type,
                SelectedPeriod = period,
                TotalCollected = totalCollected,
                TotalOutstanding = totalOutstanding,
                TotalExpectedDues = expectedDuesInPeriod,
                EfficiencyRate = displayEfficiencyRate,
                DelinquencyRate = displayDelinquencyRate,
                AmountDues = amountDues,
                AmountCerts = amountCerts,
                AmountStickers = amountStickers,
                TopDelinquents = topList,
                ChartLabels = chartLabels,
                CollectionData = collectionData,
                TargetData = targetData,
                DelinquencyData = delinquencyData,
                SnapshotData = snapshotData,
                BarColors = barColors,
                CumulativeActual = cumActual,
                CumulativeProjected = cumProj,
                ExecutiveInsight = execInsight,
                TrendsInsight = trendInsight,
                RevenueInsight = revInsight,
                DetailsInsight = detailInsight
            };

            ViewData["ReportTitle"] = $"Financial Report - {type}";
            return View("PrintFinancial", reportModel);
        }

        // ==========================================
        // 2. DHSUD REPORT (RESTORED LOGIC)
        // ==========================================
        private async Task<IActionResult> ExportDhsudReport(string format)
        {
            var rawData = await _context.Properties
                .Include(p => p.Memberships)
                .Include(p => p.Tenants)
                .OrderBy(p => p.Block).ThenBy(p => p.Lot)
                .ToListAsync();

            var members = rawData.Select(p => {
                var m = p.Memberships.FirstOrDefault();
                var t = p.Tenants?.OrderByDescending(x => x.StartDate).FirstOrDefault();
                return new
                {
                    Block = p.Block,
                    Lot = p.Lot,
                    LastName = m?.LastName ?? "VACANT",
                    FirstName = m?.FirstName ?? "",
                    MiddleName = m?.MiddleName ?? "",
                    Status = m != null ? p.Status : (t != null ? "Occupied (Tenant)" : "VACANT"),
                    Contact = m?.ContactNumber ?? "-",
                    Email = m?.ContactNumber ?? "-",
                    CivilStatus = m?.CivilStatus ?? "-",
                    Sex = m?.Sex ?? "-",
                    Dob = m?.DateOfBirth,
                    Spouse = m?.SpouseName ?? "-",
                    YearsStay = m?.YearsStay.ToString() ?? "0",
                    IsVoter = (m?.IsVoter ?? false) ? "Yes" : "No",
                    Payment = m?.PreferredPayment ?? "-",
                    Seniors = m?.CountSeniors ?? 0,
                    Helpers = m?.CountHelpers ?? 0,
                    Children = m?.CountChildren ?? 0,
                    Others = m?.CountOthers ?? 0,
                    TotalMembers = m?.TotalMembers ?? 0,
                    Dogs = m?.CountDogs ?? 0,
                    Cats = m?.CountCats ?? 0,
                    OtherPets = m?.OtherPets ?? "-",
                    TenantName = t?.FullName ?? "N/A",
                    TenantContact = t?.ContactNumber ?? "N/A"
                };
            }).ToList();

            if (format == "CSV")
            {
                var csv = new StringBuilder();
                csv.AppendLine("Block,Lot,Name,Status,Contact,Tenant Name,Tenant Contact,Civil Status,Sex,DOB,Spouse,Stay(Yrs),Voter,Payment,Seniors,Helpers,Children,Others,Total,Dogs,Cats,Other Pets");
                foreach (var m in members)
                {
                    var dobStr = m.Dob?.ToString("yyyy-MM-dd") ?? "-";
                    csv.AppendLine($"{m.Block},{m.Lot},\"{m.LastName}, {m.FirstName}\",{m.Status},{m.Contact},\"{m.TenantName}\",{m.TenantContact},{m.CivilStatus},{m.Sex},{dobStr},{m.Spouse},{m.YearsStay},{m.IsVoter},{m.Payment},{m.Seniors},{m.Helpers},{m.Children},{m.Others},{m.TotalMembers},{m.Dogs},{m.Cats},{m.OtherPets}");
                }
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "DHSUD_MasterList.csv");
            }
            else
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("DHSUD Master List");

                    var headers = new[] { "Block", "Lot", "Name", "Status", "Contact", "Tenant Name", "Tenant Contact", "Civil Status", "Sex", "DOB", "Spouse", "Stay(Yrs)", "Voter", "Payment", "Seniors", "Helpers", "Children", "Others", "Total", "Dogs", "Cats", "Other Pets" };
                    for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

                    for (int i = 0; i < members.Count; i++)
                    {
                        var m = members[i];
                        int r = i + 2;
                        ws.Cell(r, 1).Value = m.Block; ws.Cell(r, 2).Value = m.Lot;
                        ws.Cell(r, 3).Value = $"{m.LastName}, {m.FirstName}"; ws.Cell(r, 4).Value = m.Status;
                        ws.Cell(r, 5).Value = m.Contact; ws.Cell(r, 6).Value = m.TenantName;
                        ws.Cell(r, 7).Value = m.TenantContact; ws.Cell(r, 8).Value = m.CivilStatus;
                        ws.Cell(r, 9).Value = m.Sex;

                        if (m.Dob.HasValue) ws.Cell(r, 10).Value = m.Dob.Value.ToDateTime(TimeOnly.MinValue);
                        else ws.Cell(r, 10).Value = "-";

                        ws.Cell(r, 11).Value = m.Spouse; ws.Cell(r, 12).Value = m.YearsStay;
                        ws.Cell(r, 13).Value = m.IsVoter; ws.Cell(r, 14).Value = m.Payment;
                        ws.Cell(r, 15).Value = m.Seniors; ws.Cell(r, 16).Value = m.Helpers;
                        ws.Cell(r, 17).Value = m.Children; ws.Cell(r, 18).Value = m.Others;
                        ws.Cell(r, 19).Value = m.TotalMembers; ws.Cell(r, 20).Value = m.Dogs;
                        ws.Cell(r, 21).Value = m.Cats; ws.Cell(r, 22).Value = m.OtherPets;
                    }
                    ws.Columns().AdjustToContents();

                    using (var ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DHSUD_MasterList.xlsx");
                    }
                }
            }
        }
    }
}