using BCrypt.Net;
using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin")]
    public class UsersController : Controller
    {
        private readonly HoaDbContext _context;

        public UsersController(HoaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, string role, int page = 1)
        {
            int pageSize = 20;
            var query = _context.UserAccounts
            .Include(u => u.Property)
            .AsQueryable();

            // 1. Populate Dropdown
            var roles = await _context.UserAccounts.Select(u => u.Role).Distinct().ToListAsync();
            ViewBag.Roles = new SelectList(roles, role);

            // 2. Apply Filters
            if (!string.IsNullOrEmpty(searchString))
            {
                // STRICT SEARCH: Checks only Username OR Email
                query = query.Where(u => u.Username.Contains(searchString) ||
                                         u.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // 3. Pagination
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedUsers = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 4. Pass Data
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentRole = role;

            return View(pagedUsers);
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAccount user, int? blockNo, int? lotNo)
        {
            // --- 1. UNIQUE CHECKS ---
            bool usernameExists = await _context.UserAccounts.AnyAsync(u => u.Username == user.Username);
            if (usernameExists)
            {
                TempData["Error"] = $"Username '{user.Username}' is already taken.";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                bool emailExists = await _context.UserAccounts.AnyAsync(u => u.Email == user.Email);
                if (emailExists)
                {
                    TempData["Error"] = $"Email '{user.Email}' is already registered.";
                    return RedirectToAction("Index");
                }
            }

            // --- 2. BLOCK & LOT LOGIC ---
            if (blockNo.HasValue && lotNo.HasValue)
            {
                string searchBlock = blockNo.Value.ToString();
                string searchLot = lotNo.Value.ToString();

                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Block == searchBlock && p.Lot == searchLot);

                if (property != null)
                {
                    user.PropertyId = property.PropertyId;
                }
                else
                {
                    TempData["Error"] = $"Block {blockNo} Lot {lotNo} not found.";
                    return RedirectToAction("Index");
                }
            }

            // --- 3. DEFAULTS & HASHING ---
            user.IsActive = true;
            user.Last_Login = null;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass123"); // Default Password

            ModelState.Remove("Password");

            if (ModelState.IsValid)
            {
                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();

                await LogAudit("Create", user.UserId, $"Created user: {user.Username} ({user.Role})");

                TempData["Success"] = "User created successfully with default password 'pass123'.";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to create user. Please check input.";
            return RedirectToAction("Index");
        }

        // POST: Users/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserAccount user, int? blockNo, int? lotNo)
        {
            var existingUser = await _context.UserAccounts.FindAsync(user.UserId);
            if (existingUser == null) return NotFound();

            // --- 1. UNIQUE CHECKS (Exclude self) ---
            bool usernameTaken = await _context.UserAccounts
                .AnyAsync(u => u.Username == user.Username && u.UserId != user.UserId);

            if (usernameTaken)
            {
                TempData["Error"] = $"Username '{user.Username}' is already taken.";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                bool emailTaken = await _context.UserAccounts
                    .AnyAsync(u => u.Email == user.Email && u.UserId != user.UserId);

                if (emailTaken)
                {
                    TempData["Error"] = $"Email '{user.Email}' is already in use by another user.";
                    return RedirectToAction("Index");
                }
            }

            // --- 2. BLOCK & LOT UPDATE ---
            if (blockNo.HasValue && lotNo.HasValue)
            {
                string searchBlock = blockNo.Value.ToString();
                string searchLot = lotNo.Value.ToString();

                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Block == searchBlock && p.Lot == searchLot);

                if (property != null)
                {
                    existingUser.PropertyId = property.PropertyId;
                }
                else
                {
                    TempData["Error"] = "Property Block/Lot not found.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                existingUser.PropertyId = null;
            }

            // --- 3. SAVE UPDATES ---
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;

            await _context.SaveChangesAsync();
            await LogAudit("Update", existingUser.UserId, $"Updated profile for: {existingUser.Username}");

            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("Index");
        }

        // POST: Users/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            string status = user.IsActive ? "Activated" : "Deactivated";
            await LogAudit("Update", user.UserId, $"{status} user: {user.Username}");

            return RedirectToAction("Index");
        }

        // POST: Users/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass123");

            await _context.SaveChangesAsync();
            await LogAudit("Update", user.UserId, $"Reset password for: {user.Username}");

            TempData["Success"] = $"Password for {user.Username} reset to 'pass123'.";
            return RedirectToAction("Index");
        }

        // === AUDIT LOG HELPER ===
        private async Task LogAudit(string action, int recordId, string details)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int? currentUserId = null; // Use nullable int

            if (!string.IsNullOrEmpty(userIdString))
            {
                currentUserId = int.Parse(userIdString);
            }

            // If we still don't have a user (Session lost), 
            // we should try to find the "System" or "Admin" account, 
            // OR just record it as the user being modified if it's a self-update.
            if (currentUserId == null)
            {
                // If no one is logged in, we default to the Admin just so the FK doesn't crash.
                // Ideally, you should redirect to Login if Session is null.
                var adminUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser != null) currentUserId = adminUser.UserId;

                // Append a note to the details so you know why this happened
                details += " (Session Expired/System Action)";
            }

            var audit = new AuditLog
            {
                ActionType = action,
                TableName = "UserAccount",
                RecordId = recordId,
                UserId = currentUserId ?? recordId, // Fallback
                Timestamp = DateTime.Now,
                Details = details
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}