using Microsoft.AspNetCore.Mvc;
using HOA_Management_System.Models;
using Microsoft.AspNetCore.Http; // For Session
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // IMPORTANT: Add this using statement

namespace HOA_Management_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly HoaDbContext _context;

        public AccountController(HoaDbContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            // Check if session exists
            if (HttpContext.Session.GetString("UserId") != null)
            {
                var role = HttpContext.Session.GetString("Role");

                // ADDED TREASURER: Redirect to the correct portal, NOT Home/Index
                if (role == "Admin" || role == "Treasurer") return RedirectToAction("Index", "Dashboard");
                if (role == "Encoder") return RedirectToAction("EncoderIndex", "Dashboard");

                return RedirectToAction("Index", "HomeownerPortal");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string loginInput, string password)
        {
            // 1. Check for Empty Inputs
            if (string.IsNullOrEmpty(loginInput) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter your Username/Email and Password.";
                return View();
            }

            // 2. SEARCH BY USERNAME OR EMAIL
            // We look for a match in EITHER the Username column OR the Email column
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == loginInput || u.Email == loginInput);

            if (user != null)
            {
                bool isPasswordValid = false;

                // 3. Verify Password (Hash vs Plain Text fallback)
                if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash.StartsWith("$2"))
                {
                    try
                    {
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    }
                    catch (BCrypt.Net.SaltParseException) { isPasswordValid = false; }
                }
                else
                {
                    // Legacy Plain Text Support
                    if (user.PasswordHash == password)
                    {
                        isPasswordValid = true;
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password); // Auto-Upgrade
                    }
                }

                if (isPasswordValid)
                {
                    if (!user.IsActive)
                    {
                        ViewBag.Error = "Your account has been deactivated. Please contact Admin.";
                        return View();
                    }

                    // Set Session
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    user.Last_Login = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // --- ADDED TREASURER ROUTING HERE ---
                    if (user.Role == "Admin" || user.Role == "Treasurer")
                    {
                        // Admin and Treasurer go to the main Analytics Dashboard
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else if (user.Role == "Encoder")
                    {
                        // Encoder goes to the new Operational Dashboard
                        return RedirectToAction("EncoderIndex", "Dashboard");
                    }
                    else
                    {
                        // Residents go to the Home Page
                        return RedirectToAction("Index", "HomeownerPortal");
                    }
                }
            }

            // 4. DISPLAY ERROR IF FAILED
            // This line sends the message to the View
            ViewBag.Error = "Incorrect Username/Email or Password.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}