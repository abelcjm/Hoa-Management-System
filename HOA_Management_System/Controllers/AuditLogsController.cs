using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin")]
    public class AuditLogsController : Controller
    {
        private readonly HoaDbContext _context;

        public AuditLogsController(HoaDbContext context)
        {
            _context = context;
        }

        // GET: AuditLogs
        // GET: AuditLogs
        public async Task<IActionResult> Index(
            string? filterDate,
            string? filterUser,
            string? filterAction,
            string? filterModule,
            int page = 1)
        {
            int pageSize = 30;

            // 1. Start the Query
            var logsQuery = _context.AuditLogs.Include(a => a.User).AsQueryable();

            // 2. Apply Filters (One by one)

            // Date Filter (Checks if the log happened on that specific day)
            if (!string.IsNullOrEmpty(filterDate) && DateTime.TryParse(filterDate, out DateTime dateVal))
            {
                logsQuery = logsQuery.Where(l => l.Timestamp.Date == dateVal.Date);
            }

            // User Filter (Matches Username)
            if (!string.IsNullOrEmpty(filterUser))
            {
                // Changed from '==' to '.Contains()' so partial searches work
                logsQuery = logsQuery.Where(l => l.User.Username.Contains(filterUser));
            }

            // Action Filter (Create, Edit, etc.)
            if (!string.IsNullOrEmpty(filterAction))
            {
                logsQuery = logsQuery.Where(l => l.ActionType == filterAction);
            }

            // Module Filter (UserAccount, Property, etc.)
            if (!string.IsNullOrEmpty(filterModule))
            {
                logsQuery = logsQuery.Where(l => l.TableName == filterModule);
            }

            // 3. Prepare Dropdown Data (Distinct values for the UI)
            // We fetch these distinct lists so the user can just select from a menu
            ViewBag.ActionList = await _context.AuditLogs.Select(a => a.ActionType).Distinct().ToListAsync();
            ViewBag.ModuleList = await _context.AuditLogs.Select(a => a.TableName).Distinct().ToListAsync();

            // 4. Pagination Logic
            int totalLogs = await logsQuery.CountAsync();
            var pagedLogs = await logsQuery
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            // 5. Pass Data to View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

            // Remember the filters so the inputs stay filled
            ViewData["FilterDate"] = filterDate;
            ViewData["FilterUser"] = filterUser;
            ViewData["FilterAction"] = filterAction;
            ViewData["FilterModule"] = filterModule;

            return View(pagedLogs);
        }
    }
}