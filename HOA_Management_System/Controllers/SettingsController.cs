using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using HOA_Management_System.Models.ViewModels; // Correct Namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin", "Encoder", "Treasurer")]
    public class SettingsController : Controller
    {
        private readonly HoaDbContext _context;

        public SettingsController(HoaDbContext context)
        {
            _context = context;
        }

        // GET: Settings
        public async Task<IActionResult> Index(string activeTab = "profile")
        {
            int userId = GetCurrentUserId();
            var user = await _context.UserAccounts.FindAsync(userId);

            if (user == null) return RedirectToAction("Login", "Account");

            // 1. Determine Property Info Display
            string propDisplay = "No Property Assigned";

            if (user.Role == "Admin") propDisplay = "System Administrator";
            else if (user.Role == "Encoder") propDisplay = "Staff / Encoder";

            // 2. Load Profile Data
            var model = new SettingsIndexViewModel
            {
                ActiveTab = activeTab,
                Profile = new UserProfileViewModel
                {
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    PropertyInfo = propDisplay
                },
                Password = new ChangePasswordViewModel()
            };

            // 3. Load System Settings (Admin Only)
            if (user.Role == "Admin")
            {
                var rateSetting = await _context.AppSettings.FindAsync("MonthlyFee");

                // Get the rate from DB or default to 500 if missing
                decimal currentRate = rateSetting != null ? decimal.Parse(rateSetting.Value) : 500;

                model.System = new SystemConfigViewModel
                {
                    MonthlyFee = currentRate,
                    TargetAmount = currentRate,

                    StartYear = DateTime.Now.Year,
                    EndYear = DateTime.Now.Year
                };
            }

            return View(model);
        }

        // POST: UpdateProfile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(SettingsIndexViewModel model)
        {
            int userId = GetCurrentUserId();
            var user = await _context.UserAccounts.FindAsync(userId);

            if (user != null)
            {
                // Check if Username is taken (if changed)
                if (user.Username != model.Profile.Username)
                {
                    bool exists = await _context.UserAccounts.AnyAsync(u => u.Username == model.Profile.Username && u.UserId != userId);
                    if (exists)
                    {
                        TempData["Error"] = "Username is already taken.";
                        return RedirectToAction("Index", new { activeTab = "profile" });
                    }
                }

                user.Username = model.Profile.Username;
                user.Email = model.Profile.Email;

                _context.Update(user);
                await _context.SaveChangesAsync();

                // Update Session Username if changed
                HttpContext.Session.SetString("Username", user.Username);

                TempData["Success"] = "Profile updated successfully.";
            }

            return RedirectToAction("Index", new { activeTab = "profile" });
        }

        // POST: ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(SettingsIndexViewModel model)
        {
            int userId = GetCurrentUserId();
            var user = await _context.UserAccounts.FindAsync(userId);

            if (user != null)
            {
                // 1. VERIFY OLD PASSWORD (Mimicking AccountController Logic)
                bool isOldPasswordCorrect = false;
                string inputOldPass = model.Password.OldPassword;
                string storedPass = user.PasswordHash;

                // A. Try BCrypt Verify (Standard)
                if (!string.IsNullOrEmpty(storedPass) && storedPass.StartsWith("$2"))
                {
                    try
                    {
                        if (BCrypt.Net.BCrypt.Verify(inputOldPass, storedPass))
                        {
                            isOldPasswordCorrect = true;
                        }
                    }
                    catch (BCrypt.Net.SaltParseException)
                    {
                        // If salt is invalid, fall through to plain text check
                    }
                }

                // B. Fallback: Legacy Plain Text Check (Auto-Upgrade Logic)
                if (!isOldPasswordCorrect && storedPass == inputOldPass)
                {
                    isOldPasswordCorrect = true;
                }

                if (!isOldPasswordCorrect)
                {
                    TempData["Error"] = "Incorrect old password.";
                    return RedirectToAction("Index", new { activeTab = "profile" });
                }

                // 2. VERIFY NEW PASSWORDS MATCH
                if (model.Password.NewPassword != model.Password.ConfirmPassword)
                {
                    TempData["Error"] = "New passwords do not match.";
                    return RedirectToAction("Index", new { activeTab = "profile" });
                }

                // 3. HASH AND SAVE NEW PASSWORD
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password.NewPassword);

                user.PasswordHash = newPasswordHash;

                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Password changed successfully.";
            }

            return RedirectToAction("Index", new { activeTab = "profile" });
        }

        // POST: UpdateSystemSettings (GLOBAL RATE -> Current Month Onwards)
        [HttpPost]
        public async Task<IActionResult> UpdateSystemSettings(SettingsIndexViewModel model)
        {
            int userId = GetCurrentUserId();
            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null || user.Role != "Admin") return Unauthorized();

            // 1. Save the new Default Rate
            decimal newRate = model.System.MonthlyFee;
            var rateSetting = await _context.AppSettings.FindAsync("MonthlyFee");
            if (rateSetting == null)
            {
                rateSetting = new AppSetting { Key = "MonthlyFee" };
                _context.AppSettings.Add(rateSetting);
            }
            rateSetting.Value = newRate.ToString();
            await _context.SaveChangesAsync();

            // 2. TRIGGER REALLOCATION (Scope: Current Month -> Future)
            // We use the first day of the CURRENT month as the start point.
            var today = DateTime.Today;
            var startOfCurrentMonth = new DateTime(today.Year, today.Month, 1);

            await RunWaterfallReallocation(startOfCurrentMonth, null, newRate, true);

            TempData["Success"] = "Global rate updated. Reallocation applied from current month onwards. Advance payers were grandfathered in.";
            return RedirectToAction("Index", new { activeTab = "system" });
        }

        // POST: GenerateBillingRecords (TARGETED -> Specific Range Only)
        [HttpPost]
        public async Task<IActionResult> GenerateBillingRecords(SettingsIndexViewModel model)
        {
            int userId = GetCurrentUserId();
            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null || user.Role != "Admin") return Unauthorized();

            // 1. Define Range
            var startDate = new DateTime(model.System.StartYear, model.System.StartMonth, 1);
            var endDate = new DateTime(model.System.EndYear, model.System.EndMonth, 1);
            decimal targetRate = model.System.TargetAmount;

            if (startDate > endDate)
            {
                TempData["Error"] = "Start Date cannot be after End Date.";
                return RedirectToAction("Index", new { activeTab = "system" });
            }

            // 2. TRIGGER REALLOCATION (Scope: Range Only)
            // We pass the EndDate so the logic knows when to STOP changing the price tag.
            // The 'isGlobal' flag is false.
            await RunWaterfallReallocation(startDate, endDate, targetRate, false);

            TempData["Success"] = $"Billing records updated for {startDate:MMM yyyy} - {endDate:MMM yyyy}. Payments reallocated.";
            return RedirectToAction("Index", new { activeTab = "system" });
        }

        // ============================================================
        //  SHARED CORE LOGIC: THE WATERFALL ENGINE
        // ============================================================
        private async Task RunWaterfallReallocation(DateTime startDate, DateTime? endDate, decimal newRate, bool isGlobalChange)
        {
            var rateSetting = await _context.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == "MonthlyFee");
            decimal globalDefaultRate = rateSetting != null ? decimal.Parse(rateSetting.Value) : 500;

            var properties = await _context.Properties.ToListAsync();

            foreach (var prop in properties)
            {
                // A. FETCH RELEVANT BILLS (Target Range Onwards)
                var billsToProcess = await _context.MonthlyDues
                    .Where(d => d.PropertyId == prop.PropertyId
                                && (d.Year > startDate.Year || (d.Year == startDate.Year && d.Month >= startDate.Month)))
                    .OrderBy(d => d.Year).ThenBy(d => d.Month)
                    .ToListAsync();

                // B. LIQUIDATE WALLET (CORRECTED LOGIC)
                // 1. Get ALL money ever paid for dues
                var transactions = await _context.Transactions
                    .Where(t => t.PropertyId == prop.PropertyId &&
                               (t.PaymentType == "MonthlyDue" || t.PaymentType == "Monthly Dues"))
                    .ToListAsync();

                decimal totalMoneyEverPaid = transactions.Sum(t => t.Amount);

                // 2. Subtract money already consumed by PAST bills (bills before our startDate)
                var pastBills = await _context.MonthlyDues
                    .Where(d => d.PropertyId == prop.PropertyId
                             && (d.Year < startDate.Year || (d.Year == startDate.Year && d.Month < startDate.Month)))
                    .ToListAsync();

                decimal moneyConsumedByPast = pastBills.Sum(d => d.TotalPaid ?? 0);

                // 3. The true remaining wallet for the bills we are about to process
                decimal wallet = totalMoneyEverPaid - moneyConsumedByPast;

                // C. RE-STRUCTURE EXISTING BILLS IN RANGE
                var loopEnd = isGlobalChange
                    ? (billsToProcess.Any() ? new DateTime(billsToProcess.Last().Year, billsToProcess.Last().Month, 1) : startDate)
                    : endDate.Value;

                var cursor = startDate;

                while (cursor <= loopEnd || (isGlobalChange && cursor <= loopEnd))
                {
                    var bill = billsToProcess.FirstOrDefault(b => b.Month == cursor.Month && b.Year == cursor.Year);
                    bool shouldUpdateRate = isGlobalChange || (endDate.HasValue && cursor <= endDate.Value);

                    if (bill == null)
                    {
                        if (shouldUpdateRate)
                        {
                            bill = new MonthlyDue
                            {
                                PropertyId = prop.PropertyId,
                                Month = cursor.Month,
                                Year = cursor.Year,
                                AmountDue = newRate,
                                TotalPaid = 0,
                                Status = "Unpaid"
                            };
                            _context.MonthlyDues.Add(bill);
                            billsToProcess.Add(bill);
                        }
                    }
                    else
                    {
                        // --- THE GOLDEN RULE: FAIRNESS CHECK ---
                        bool hasAdvancePayment = (bill.TotalPaid ?? 0) > 0;

                        if (shouldUpdateRate && !hasAdvancePayment)
                        {
                            bill.AmountDue = newRate;
                        }
                        // If they have advance payment, AmountDue stays locked!

                        bill.TotalPaid = 0; // Wipe so we can refill from the clean wallet
                        bill.Status = "Unpaid";
                        _context.Update(bill);
                    }

                    cursor = cursor.AddMonths(1);
                    if (isGlobalChange && bill == null && wallet <= 0) break;
                }

                // Reset bills outside targeted range (if not global)
                foreach (var bill in billsToProcess)
                {
                    var billDate = new DateTime(bill.Year, bill.Month, 1);
                    if (!isGlobalChange && endDate.HasValue && billDate > endDate.Value)
                    {
                        bill.TotalPaid = 0;
                        bill.Status = "Unpaid";
                        _context.Update(bill);
                    }
                }

                // D. WATERFALL DISTRIBUTION
                billsToProcess = billsToProcess.OrderBy(b => b.Year).ThenBy(b => b.Month).ToList();

                foreach (var bill in billsToProcess)
                {
                    if (wallet <= 0) break;
                    decimal needed = bill.AmountDue;

                    if (needed <= 0) { bill.TotalPaid = 0; bill.Status = "Paid"; continue; }

                    if (wallet >= needed) { bill.TotalPaid = needed; bill.Status = "Paid"; wallet -= needed; }
                    else { bill.TotalPaid = wallet; bill.Status = "Partial"; wallet = 0; }
                }

                // E. HANDLE SURPLUS (Expansion for overpayments)
                var lastKnownBill = billsToProcess.LastOrDefault();
                var nextDate = lastKnownBill != null
                    ? new DateTime(lastKnownBill.Year, lastKnownBill.Month, 1).AddMonths(1)
                    : startDate;

                while (wallet > 0 && nextDate.Year < 2100)
                {
                    decimal expansionRate = isGlobalChange ? newRate : globalDefaultRate;
                    if (expansionRate <= 0) break;

                    decimal pay = Math.Min(wallet, expansionRate);
                    var newBill = new MonthlyDue
                    {
                        PropertyId = prop.PropertyId,
                        Month = nextDate.Month,
                        Year = nextDate.Year,
                        AmountDue = expansionRate,
                        TotalPaid = pay,
                        Status = pay >= expansionRate ? "Paid" : "Partial"
                    };
                    _context.MonthlyDues.Add(newBill);
                    billsToProcess.Add(newBill);
                    wallet -= pay;
                    nextDate = nextDate.AddMonths(1);
                }

                // F. CLEANUP: Delete "Ghost" Bills
                var today = DateTime.Today;
                var currentMonthStart = new DateTime(today.Year, today.Month, 1);

                var safeZoneEnd = isGlobalChange ? currentMonthStart : (endDate ?? currentMonthStart);

                var lastPaidBill = billsToProcess.LastOrDefault(b => b.TotalPaid > 0 || b.Status == "Paid");
                if (lastPaidBill != null)
                {
                    var lastPaidDate = new DateTime(lastPaidBill.Year, lastPaidBill.Month, 1);
                    if (lastPaidDate > safeZoneEnd) safeZoneEnd = lastPaidDate;
                }

                var ghostBills = billsToProcess
                    .Where(b => b.TotalPaid == 0
                             && b.Status != "Paid"
                             && new DateTime(b.Year, b.Month, 1) > safeZoneEnd
                             && new DateTime(b.Year, b.Month, 1) > currentMonthStart)
                    .ToList();

                if (ghostBills.Any())
                {
                    _context.MonthlyDues.RemoveRange(ghostBills);
                }
            }

            await _context.SaveChangesAsync();
        }

        // --- Helper Methods ---

        private int GetCurrentUserId()
        {
            var idStr = HttpContext.Session.GetString("UserId");
            return idStr != null ? int.Parse(idStr) : 0;
        }
    }
}