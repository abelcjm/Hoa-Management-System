using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin", "Encoder")]
    public class PaymentsController : Controller
    {
        private readonly HoaDbContext _context;

        public PaymentsController(HoaDbContext context)
        {
            _context = context;
        }

        // 1. INDEX (With Recent Transactions)
        public async Task<IActionResult> Index(int? preSelectId, string preType)
        {
            var properties = await _context.Properties
                .Include(p => p.Memberships)
                .Select(p => new {
                    p.PropertyId,
                    OwnerName = p.Memberships.Any(m => m.IsCurrentOwner)
                                ? p.Memberships.FirstOrDefault(m => m.IsCurrentOwner)!.LastName + ", " + p.Memberships.FirstOrDefault(m => m.IsCurrentOwner)!.FirstName
                                : "No Owner",
                    p.Block,
                    p.Lot
                })
                .ToListAsync();

            var listItems = properties.Select(p => new {
                PropertyId = p.PropertyId,
                DisplayText = $"B{p.Block} L{p.Lot} - {p.OwnerName}"
            });

            ViewBag.Properties = new SelectList(listItems, "PropertyId", "DisplayText", preSelectId);

            var recentTransactions = await _context.Transactions
                .Include(t => t.Property)
                .OrderByDescending(t => t.TransactionId)
                .Take(12)
                .Select(t => new {
                    t.PaymentDate,
                    t.OrNumber,
                    t.PaymentType,
                    t.Amount,
                    Block = t.Property.Block,
                    Lot = t.Property.Lot,
                    Encoder = _context.UserAccounts
                                .Where(u => u.UserId == t.EncodedBy)
                                .Select(u => u.Username)
                                .FirstOrDefault() ?? "Unknown"
                })
                .ToListAsync();

            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.PreSelectId = preSelectId;
            ViewBag.PreType = preType;

            return View();
        }

        // 2. GET PENDING BILLS API
        [HttpGet]
        public async Task<IActionResult> GetPendingBills(int propertyId)
        {
            var today = DateTime.Today;

            var bills = await _context.MonthlyDues
               .Where(d => d.PropertyId == propertyId
                        && d.Status != "Paid"
                        && (d.Year < today.Year || (d.Year == today.Year && d.Month <= today.Month)))
               .OrderBy(d => d.Year).ThenBy(d => d.Month)
               .Select(d => new {
                   d.Year,
                   d.Month,
                   AmountDue = d.AmountDue,
                   PaidSoFar = d.TotalPaid ?? 0,
                   Balance = d.AmountDue - (d.TotalPaid ?? 0),
                   Status = d.Status
               })
               .ToListAsync();

            var lastPaidBill = await _context.MonthlyDues
                .Where(d => d.PropertyId == propertyId && d.TotalPaid > 0)
                .OrderByDescending(d => d.Year).ThenByDescending(d => d.Month)
                .FirstOrDefaultAsync();

            string paidUntilStr = "No Payment History";
            if (lastPaidBill != null)
            {
                paidUntilStr = new DateTime(lastPaidBill.Year, lastPaidBill.Month, 1).ToString("MMMM yyyy");
                if (lastPaidBill.Status == "Partial") paidUntilStr += " (Partial)";
            }

            decimal advancedAmount = await _context.MonthlyDues
                .Where(d => d.PropertyId == propertyId &&
                           (d.Year > today.Year || (d.Year == today.Year && d.Month > today.Month)))
                .SumAsync(d => d.TotalPaid ?? 0);

            return Json(new { bills = bills, paidUntil = paidUntilStr, advancedTotal = advancedAmount });
        }

        // 3. CREATE (Process Payment)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int PropertyId, decimal Amount, string PaymentType, string ReferenceNo, DateOnly PaymentDate, string RequestorName, string CertificationType, string OtherCertificationType)
        {
            if (string.IsNullOrWhiteSpace(ReferenceNo))
            {
                TempData["Error"] = "Reference / OR Number is required.";
                return RedirectToAction(nameof(Index));
            }

            bool orExists = await _context.Transactions.AnyAsync(t => t.OrNumber == ReferenceNo);
            if (orExists)
            {
                TempData["Error"] = $"OR Number '{ReferenceNo}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            if (Amount <= 0)
            {
                TempData["Error"] = "Payment amount must be greater than zero.";
                return RedirectToAction(nameof(Index));
            }

            string userIdStr = HttpContext.Session.GetString("UserId") ?? "1";
            int encodedBy = int.Parse(userIdStr);
            string transactionNotes = "";

            if (PaymentType == "MonthlyDue")
            {
                var unpaidBills = await _context.MonthlyDues
                    .Where(d => d.PropertyId == PropertyId && d.Status != "Paid")
                    .OrderBy(d => d.Year).ThenBy(d => d.Month)
                    .ToListAsync();

                decimal moneyLeft = Amount;
                var paidMonthsDescription = new List<string>();

                foreach (var bill in unpaidBills)
                {
                    if (moneyLeft <= 0) break;
                    decimal balance = bill.AmountDue - (bill.TotalPaid ?? 0);

                    if ((bill.TotalPaid ?? 0) == 0 && moneyLeft < balance)
                    {
                        TempData["Error"] = $"Partial payments not allowed for new months (Need ₱{balance}).";
                        return RedirectToAction(nameof(Index));
                    }

                    decimal payAmount = Math.Min(moneyLeft, balance);
                    bill.TotalPaid = (bill.TotalPaid ?? 0) + payAmount;

                    if (bill.TotalPaid >= bill.AmountDue)
                    {
                        bill.Status = "Paid";
                        bill.DateFullyPaid = PaymentDate;
                    }
                    else
                    {
                        bill.Status = "Partial";
                    }

                    moneyLeft -= payAmount;
                    paidMonthsDescription.Add($"{bill.Month}/{bill.Year}");
                }

                if (moneyLeft > 0)
                {
                    var rateSetting = await _context.AppSettings.FindAsync("MonthlyFee");
                    decimal currentRate = 500;
                    if (rateSetting != null && decimal.TryParse(rateSetting.Value, out decimal parsedRate)) currentRate = parsedRate;

                    var lastBill = await _context.MonthlyDues.Where(d => d.PropertyId == PropertyId).OrderByDescending(d => d.Year).ThenByDescending(d => d.Month).FirstOrDefaultAsync();
                    DateTime nextDate = lastBill != null ? new DateTime(lastBill.Year, lastBill.Month, 1).AddMonths(1) : DateTime.Today;

                    while (moneyLeft > 0 && nextDate.Year < 3000)
                    {
                        decimal cost = currentRate;
                        decimal pay = Math.Min(moneyLeft, cost);
                        bool isFullyPaid = pay >= cost;

                        _context.MonthlyDues.Add(new MonthlyDue
                        {
                            PropertyId = PropertyId,
                            Month = nextDate.Month,
                            Year = nextDate.Year,
                            AmountDue = cost,
                            TotalPaid = pay,
                            Status = isFullyPaid ? "Paid" : "Partial",
                            DateFullyPaid = isFullyPaid ? PaymentDate : null
                        });

                        moneyLeft -= pay;
                        paidMonthsDescription.Add($"{nextDate.Month}/{nextDate.Year} (Adv)");
                        nextDate = nextDate.AddMonths(1);
                    }
                }
                transactionNotes = $"Monthly Dues: " + string.Join(", ", paidMonthsDescription.Take(5)) + (paidMonthsDescription.Count > 5 ? "..." : "");

                // --- NEW DELINQUENCY RESOLUTION LOGIC ---
                // Calculate if they still have arrears using the IN-MEMORY list we just updated!
                var remainingArrears = unpaidBills
                    .Where(d => d.Year < PaymentDate.Year || (d.Year == PaymentDate.Year && d.Month <= PaymentDate.Month))
                    .Sum(d => d.AmountDue - (d.TotalPaid ?? 0));

                if (remainingArrears <= 0)
                {
                    var currentOwner = await _context.Memberships.FirstOrDefaultAsync(m => m.PropertyId == PropertyId && m.IsCurrentOwner);
                    if (currentOwner != null)
                    {
                        var openDelinquencies = await _context.DelinquencyHistories
                            .Where(dh => dh.MembershipId == currentOwner.MembershipId && dh.DateResolved == null)
                            .ToListAsync();

                        foreach (var record in openDelinquencies)
                        {
                            record.DateResolved = DateTime.Now; // Mark as resolved!
                        }
                    }
                }
            }
            else if (PaymentType == "Sticker")
            {
                transactionNotes = "Vehicle Sticker Payment";
            }
            else if (PaymentType == "Certification")
            {
                if (string.IsNullOrWhiteSpace(RequestorName) || string.IsNullOrWhiteSpace(CertificationType))
                {
                    TempData["Error"] = "Requestor Name and Certification Type are required.";
                    return RedirectToAction(nameof(Index));
                }

                string finalCertType = CertificationType;
                if (CertificationType == "Other")
                {
                    if (string.IsNullOrWhiteSpace(OtherCertificationType))
                    {
                        TempData["Error"] = "Please specify the Other Certification Type.";
                        return RedirectToAction(nameof(Index));
                    }
                    finalCertType = OtherCertificationType;
                }

                transactionNotes = $"{finalCertType} - Requestor: {RequestorName}";

                var cert = new Certification
                {
                    PropertyId = PropertyId,
                    RequestorName = RequestorName,
                    CertType = finalCertType,
                    FeeAmount = Amount,
                    DateIssued = PaymentDate,
                    IssuedBy = encodedBy
                };
                _context.Certifications.Add(cert);
            }

            var transaction = new Transaction
            {
                PropertyId = PropertyId,
                Amount = Amount,
                PaymentDate = PaymentDate,
                PaymentType = PaymentType,
                OrNumber = ReferenceNo,
                EncodedBy = encodedBy,
                Notes = transactionNotes
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var property = await _context.Properties.FindAsync(PropertyId);
            string propDetails = property != null ? $"Block {property.Block} Lot {property.Lot}" : $"ID {PropertyId}";

            var auditLog = new AuditLog
            {
                UserId = encodedBy,
                ActionType = "Add",
                TableName = "Transactions",
                RecordId = transaction.TransactionId,
                Timestamp = DateTime.Now,
                Details = $"Collected ₱{Amount:N2} ({PaymentType}) from {propDetails}. OR#: {ReferenceNo}."
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment processed successfully!";

            if (PaymentType == "Sticker")
            {
                return RedirectToAction("Details", "Properties", new { id = PropertyId, activeTab = "vehicle", openModal = "true" });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}