using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using HOA_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Homeowner")]
    public class HomeownerPortalController : Controller
    {
        private readonly HoaDbContext _context;

        public HomeownerPortalController(HoaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            int userId = int.Parse(userIdStr);

            // Fetch User & Property
            var userAccount = await _context.UserAccounts
                .Include(u => u.Property)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userAccount == null || userAccount.Property == null) return View("NoPropertyAssigned");

            var prop = userAccount.Property;
            var member = await _context.Memberships.FirstOrDefaultAsync(m => m.PropertyId == prop.PropertyId);

            // Bills & History
            var unpaid = await _context.MonthlyDues
                .Where(d => d.PropertyId == prop.PropertyId && d.Status != "Paid")
                .OrderByDescending(d => d.Year).ThenByDescending(d => d.Month)
                .ToListAsync();

            var history = await _context.Transactions
                .Where(t => t.PropertyId == prop.PropertyId)
                .OrderByDescending(t => t.PaymentDate)
                .ToListAsync();

            var model = new HomeownerPortalViewModel
            {
                FullName = member != null ? $"{member.FirstName} {member.LastName}" : "Homeowner",
                BlockLot = $"B{prop.Block}-L{prop.Lot}",
                Email = userAccount.Username, // Using Username/Email for display
                TotalBalance = unpaid.Sum(u => u.AmountDue - (u.TotalPaid ?? 0)),
                UnpaidBills = unpaid,
                PaymentHistory = history
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(string NewUsername, string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            int userId = int.Parse(userIdStr);

            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            // 1. Password Confirmation Check
            if (!string.IsNullOrEmpty(NewPassword) && NewPassword != ConfirmNewPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return RedirectToAction("Index");
            }

            // 2. Verify Current Password
            bool isPasswordCorrect = false;
            try
            {
                isPasswordCorrect = BCrypt.Net.BCrypt.Verify(CurrentPassword, user.PasswordHash);
            }
            catch
            {
                isPasswordCorrect = (user.PasswordHash == CurrentPassword);
            }

            if (!isPasswordCorrect)
            {
                TempData["Error"] = "Incorrect current password. Changes not saved.";
                return RedirectToAction("Index");
            }

            // 3. Update Username
            if (!string.IsNullOrEmpty(NewUsername) && user.Username != NewUsername)
            {
                if (await _context.UserAccounts.AnyAsync(u => u.Username == NewUsername && u.UserId != userId))
                {
                    TempData["Error"] = "Username is already taken.";
                    return RedirectToAction("Index");
                }
                user.Username = NewUsername;
                HttpContext.Session.SetString("Username", NewUsername);
            }

            // 4. Update Password
            if (!string.IsNullOrEmpty(NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Account updated successfully!";
            return RedirectToAction("Index");
        }
    }
}