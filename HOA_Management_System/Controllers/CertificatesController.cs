using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin", "Encoder", "Treasurer")]
    public class CertificatesController : Controller
    {
        private readonly HoaDbContext _context;

        public CertificatesController(HoaDbContext context)
        {
            _context = context;
        }

        // Added 'page' parameter, default to 1
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 20; // Limit per page
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            // 1. Fetch ALL data first (needed for Dashboard calculations)
            var allCerts = await _context.Certifications
                .Include(c => c.IssuedByNavigation)
                .OrderByDescending(c => c.DateIssued)
                .AsNoTracking()
                .ToListAsync();

            // 2. Dashboard Calculations (Based on FULL data)
            ViewBag.TotalIssuedYear = allCerts.Count(c => c.DateIssued?.Year == currentYear);

            ViewBag.IssuedThisMonth = allCerts.Count(c => c.DateIssued?.Year == currentYear && c.DateIssued?.Month == currentMonth);

            ViewBag.TotalRevenue = allCerts
                .Where(c => c.DateIssued?.Year == currentYear)
                .Sum(c => c.FeeAmount);

            ViewBag.MostRequested = allCerts.GroupBy(c => c.CertType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "None";

            ViewBag.MostRequestedCount = allCerts.Where(c => c.CertType == ViewBag.MostRequested).Count();

            // 3. Pagination Logic
            int totalItems = allCerts.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Slice the data for the Table View
            var pagedCerts = allCerts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 4. Pass Pagination Data to View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedCerts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCertificate(Certification cert, string BlockNo, string LotNo)
        {
            // 1. FIND PROPERTY ID based on Block/Lot
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Block == BlockNo && p.Lot == LotNo);

            if (property == null)
            {
                // Add error if property not found (you can improve UI later to show this error)
                TempData["Error"] = "Property Block/Lot not found!";
                return RedirectToAction(nameof(Index));
            }

            // 2. Set the PropertyId found
            cert.PropertyId = property.PropertyId;

            // 3. Set User & Date
            var userId = HttpContext.Session.GetString("UserId");
            cert.IssuedBy = int.Parse(userId ?? "1");
            cert.DateIssued = DateOnly.FromDateTime(DateTime.Now);

            _context.Certifications.Add(cert);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}