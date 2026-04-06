using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using HOA_Management_System.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin", "Encoder", "Treasurer")]
    public class PropertiesController : Controller
    {
        private readonly HoaDbContext _context;

        public PropertiesController(HoaDbContext context)
        {
            _context = context;
        }

        // GET: Properties
        public async Task<IActionResult> Index(string searchString, bool showWithBalance, int page = 1)
        {
            int pageSize = 30;
            var today = DateTime.Today;
            int currentYear = today.Year;
            int currentMonth = today.Month;

            var properties = await _context.Properties
                .Include(p => p.Memberships)
                .Include(p => p.MonthlyDues)
                .AsNoTracking()
                .ToListAsync();

            var model = properties.Select(p =>
            {
                decimal arrears = p.MonthlyDues
                    .Where(d => (d.Year < currentYear || (d.Year == currentYear && d.Month <= currentMonth)) && d.Status != "Paid")
                    .Sum(d => d.AmountDue - (d.TotalPaid ?? 0));

                decimal advanced = p.MonthlyDues
                    .Where(d => d.Year > currentYear || (d.Year == currentYear && d.Month > currentMonth))
                    .Sum(d => d.TotalPaid ?? 0);

                var currentOwner = p.Memberships.FirstOrDefault(m => m.IsCurrentOwner);

                return new PropertyViewModel
                {
                    PropertyId = p.PropertyId,
                    BlockLot = $"B{p.Block}-L{p.Lot}",
                    HomeownerName = currentOwner != null
                        ? $"{currentOwner.FirstName} {currentOwner.LastName}"
                        : "No Owner",
                    Status = p.Status,
                    Balance = arrears > 0 ? arrears : (advanced > 0 ? -advanced : 0)
                };
            }).ToList();

            if (!string.IsNullOrEmpty(searchString))
            {
                string search = searchString.Trim().ToLower();
                model = model.Where(p =>
                    p.BlockLot.ToLower().Contains(search) ||
                    p.BlockLot.Replace("B", "").Replace("L", "").ToLower().Contains(search) ||
                    p.HomeownerName.ToLower().Contains(search)
                ).ToList();
            }

            if (showWithBalance)
            {
                model = model.Where(m => m.Balance > 0).ToList();
            }

            int totalItems = model.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedModel = model
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentSearch"] = searchString;
            ViewData["ShowBalance"] = showWithBalance;

            return View(pagedModel);
        }

        // GET: Properties/Details/5
        public async Task<IActionResult> Details(int? id, string activeTab = "home", int vPage = 1, int cPage = 1, int tPage = 1, int dPage = 1, int tenPage = 1)
        {
            if (id == null) return NotFound();

            var property = await _context.Properties
                .Include(p => p.Memberships)
                .Include(p => p.MonthlyDues)
                .Include(p => p.Vehicles)
                .Include(p => p.Tenants)
                .Include(p => p.Certifications.OrderByDescending(c => c.DateIssued))
                .Include(p => p.Transactions.OrderByDescending(t => t.PaymentDate))
                .Include(p => p.Vehicles.OrderByDescending(v => v.StickerYear))
                .FirstOrDefaultAsync(m => m.PropertyId == id); // Notice: Removed AsNoTracking to allow updates

            if (property == null) return NotFound();

            var today = DateTime.Today;
            var currentYear = today.Year;
            var currentMonth = today.Month;

            decimal totalPaidThisYear = property.Transactions
                .Where(t => t.PaymentDate.Year == currentYear)
                .Sum(t => t.Amount);

            decimal arrears = property.MonthlyDues
                .Where(d => (d.Year < currentYear || (d.Year == currentYear && d.Month <= currentMonth)) && d.Status != "Paid")
                .Sum(d => d.AmountDue - (d.TotalPaid ?? 0));

            decimal advanced = property.MonthlyDues
                .Where(d => d.Year > currentYear || (d.Year == currentYear && d.Month > currentMonth))
                .Sum(d => d.TotalPaid ?? 0);

            string paidUntilStr = "";

            if (arrears == 0 && advanced > 0)
            {
                var lastPaidBill = property.MonthlyDues
                    .Where(d => d.TotalPaid > 0)
                    .OrderByDescending(d => d.Year).ThenByDescending(d => d.Month)
                    .FirstOrDefault();

                if (lastPaidBill != null)
                {
                    paidUntilStr = new DateTime(lastPaidBill.Year, lastPaidBill.Month, 1).ToString("MMM yyyy");
                    if (lastPaidBill.Status == "Partial") paidUntilStr += " (Partial)";
                }
            }

            // --- NEW: DELINQUENCY GRACE PERIOD LOGIC ---
            var currentOwner = property.Memberships.FirstOrDefault(m => m.IsCurrentOwner);
            bool isDelinquent = false;

            if (arrears > 0 && currentOwner != null)
            {
                if (currentOwner.MoveInDate.HasValue)
                {
                    // How many days since they moved in?
                    var daysSinceMoveIn = (today - currentOwner.MoveInDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays;

                    // If it's been more than 30 days, they are officially delinquent
                    if (daysSinceMoveIn > 30) { isDelinquent = true; }
                }
                else
                {
                    // If no move-in date was ever set, default to delinquent
                    isDelinquent = true;
                }
            }

            if (isDelinquent && currentOwner != null)
            {
                // Check if they already have an open delinquency record
                bool hasOpenRecord = await _context.DelinquencyHistories
                    .AnyAsync(dh => dh.MembershipId == currentOwner.MembershipId && dh.DateResolved == null);

                if (!hasOpenRecord)
                {
                    // Declare them delinquent!
                    _context.DelinquencyHistories.Add(new DelinquencyHistory
                    {
                        MembershipId = currentOwner.MembershipId,
                        PropertyId = property.PropertyId,
                        DateDeclared = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }
            }

            // Load histories to pass to the view
            var delinquencyHistories = new List<DelinquencyHistory>();
            if (currentOwner != null)
            {
                delinquencyHistories = await _context.DelinquencyHistories
                    .Where(dh => dh.MembershipId == currentOwner.MembershipId)
                    .OrderByDescending(dh => dh.DateDeclared)
                    .ToListAsync();
            }
            ViewBag.DelinquencyHistories = delinquencyHistories;


            ViewBag.TotalPaidThisYear = totalPaidThisYear;
            ViewBag.CurrentYear = currentYear;

            if (arrears > 0)
            {
                ViewBag.StatusText = "Arrears";
                ViewBag.StatusBadgeClass = "bg-danger";
                ViewBag.OutstandingColor = "text-danger";
                ViewBag.OutstandingText = "₱ " + arrears.ToString("N2");
            }
            else if (advanced > 0)
            {
                ViewBag.StatusText = $"Advanced (Until {paidUntilStr})";
                ViewBag.StatusBadgeClass = "bg-success";
                ViewBag.OutstandingColor = "text-success";
                ViewBag.OutstandingText = "(Adv) ₱ " + advanced.ToString("N2");
            }
            else
            {
                ViewBag.StatusText = "Up to Date";
                ViewBag.StatusBadgeClass = "bg-success";
                ViewBag.OutstandingColor = "text-success";
                ViewBag.OutstandingText = "₱ 0.00";
            }

            ViewBag.ActiveTab = activeTab;
            ViewBag.VehiclePage = vPage;
            ViewBag.CertPage = cPage;
            ViewBag.TransPage = tPage;
            ViewBag.DuePage = dPage;
            ViewBag.TenantPage = tenPage;
            ViewBag.PageSize = 15;

            return View(property);
        }

        // POST: UpdateHomeowner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHomeowner(Membership membership)
        {
            int currentUserId = GetCurrentUserId();
            string actionType = "";
            string details = "";

            var existing = await _context.Memberships.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MembershipId == membership.MembershipId);

            if (existing != null)
            {
                actionType = "Edit";
                details = $"Updated homeowner profile: {membership.FirstName} {membership.LastName}";
                _context.Update(membership);
            }
            else
            {
                actionType = "Add";
                details = $"Added new homeowner: {membership.FirstName} {membership.LastName}";
                membership.IsCurrentOwner = true;
                _context.Add(membership);
            }

            var log = new AuditLog
            {
                ActionType = actionType,
                TableName = "Membership",
                RecordId = membership.MembershipId,
                UserId = currentUserId,
                Timestamp = DateTime.Now,
                Details = details
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = membership.PropertyId, activeTab = "home" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuth("Admin", "Encoder")]
        public async Task<IActionResult> TransferOwnership(int propertyId, DateTime moveOutDate, string newOwnerFirstName, string newOwnerLastName)
        {
            var property = await _context.Properties
                .Include(p => p.Memberships)
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId);

            if (property == null) return NotFound();

            var currentOwners = property.Memberships.Where(m => m.IsCurrentOwner).ToList();
            foreach (var oldOwner in currentOwners)
            {
                oldOwner.IsCurrentOwner = false;
                oldOwner.MoveOutDate = DateOnly.FromDateTime(moveOutDate);
            }

            var newOwner = new Membership
            {
                PropertyId = propertyId,
                FirstName = newOwnerFirstName,
                LastName = newOwnerLastName,
                IsCurrentOwner = true,
                MoveInDate = DateOnly.FromDateTime(moveOutDate.AddDays(1)),
                ContactNumber = "",
            };

            _context.Memberships.Add(newOwner);

            var auditLog = new AuditLog
            {
                ActionType = "Transfer",
                TableName = "Properties",
                RecordId = propertyId,
                UserId = GetCurrentUserId(),
                Timestamp = DateTime.Now,
                Details = $"Property transferred to {newOwnerFirstName} {newOwnerLastName}."
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Ownership transferred successfully! Please update the new owner's full details.";

            return RedirectToAction("Details", new { id = propertyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVehicle(Vehicle vehicle)
        {
            int currentUserId = GetCurrentUserId();

            if (vehicle.PropertyId == 0) return RedirectToAction("Index");

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var log = new AuditLog
            {
                ActionType = "Add",
                TableName = "Vehicle",
                RecordId = vehicle.VehicleId,
                UserId = currentUserId,
                Timestamp = DateTime.Now,
                Details = $"Added new vehicle: {vehicle.PlateNumber} ({vehicle.VehicleType})"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = vehicle.PropertyId, activeTab = "vehicle", vPage = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVehicle(Vehicle vehicle)
        {
            int currentUserId = GetCurrentUserId();

            var existing = await _context.Vehicles.FindAsync(vehicle.VehicleId);
            if (existing == null) return NotFound();

            existing.VehicleType = vehicle.VehicleType;
            existing.PlateNumber = vehicle.PlateNumber;
            existing.Make = vehicle.Make;
            existing.Model = vehicle.Model;
            existing.Color = vehicle.Color;
            existing.VehicleOwner = vehicle.VehicleOwner;
            existing.StickerNumber = vehicle.StickerNumber;
            existing.StickerYear = vehicle.StickerYear;

            var log = new AuditLog
            {
                ActionType = "Edit",
                TableName = "Vehicle",
                RecordId = vehicle.VehicleId,
                UserId = currentUserId,
                Timestamp = DateTime.Now,
                Details = $"Updated vehicle: {vehicle.PlateNumber}"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = vehicle.PropertyId, activeTab = "vehicle" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTenant(Tenant tenant)
        {
            int currentUserId = GetCurrentUserId();

            if (tenant.PropertyId == 0) return RedirectToAction("Index");

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            var log = new AuditLog
            {
                ActionType = "Add",
                TableName = "Tenant",
                UserId = currentUserId,
                RecordId = tenant.TenantId,
                Timestamp = DateTime.Now,
                Details = $"Added new tenant: {tenant.FullName}"
            };
            _context.AuditLogs.Add(log);

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = tenant.PropertyId, activeTab = "tenant", tenPage = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTenant(Tenant tenant)
        {
            int currentUserId = GetCurrentUserId();

            var existing = await _context.Tenants.FindAsync(tenant.TenantId);
            if (existing == null) return NotFound();

            existing.FullName = tenant.FullName;
            existing.ContactNumber = tenant.ContactNumber;
            existing.StartDate = tenant.StartDate;
            existing.EndDate = tenant.EndDate;
            existing.Status = tenant.Status;

            var log = new AuditLog
            {
                ActionType = "Edit",
                TableName = "Tenant",
                RecordId = tenant.TenantId,
                UserId = currentUserId,
                Timestamp = DateTime.Now,
                Details = $"Updated tenant details: {tenant.FullName}"
            };
            _context.AuditLogs.Add(log);

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = tenant.PropertyId, activeTab = "tenant" });
        }

        private int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdString))
            {
                return int.Parse(userIdString);
            }
            var admin = _context.UserAccounts.FirstOrDefault(u => u.Role == "Admin");
            return admin != null ? admin.UserId : 1;
        }
    }
}