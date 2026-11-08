using HOA_Management_System.Filters;
using HOA_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA_Management_System.Controllers
{
    [UserAuth("Admin")]
    public class DummyDataController : Controller
    {
        private readonly HoaDbContext _context;

        public DummyDataController(HoaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Generate()
        {
            var rand = new Random();
            int startYear = 2022;
            int currentYear = 2026;

            // STRICT TIME BOUNDARIES
            DateTime today = DateTime.Today;
            DateTime now = DateTime.Now;
            int currentMonth = today.Month;

            int propsCreated = 0, membersCreated = 0, tenantsCreated = 0;
            int certsCreated = 0, vehiclesCreated = 0, duesCreated = 0, transCreated = 0;

            if (await _context.Properties.AnyAsync())
            {
                return Content("Data Generation Aborted: Records already exist. Clear the database first using the SQL truncate script.");
            }

            // ==========================================
            // 0. ENSURE ADMIN EXISTS FOR ENCODING
            // ==========================================
            var adminUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (adminUser == null)
            {
                adminUser = new UserAccount { Username = "admin_dummy", PasswordHash = BCrypt.Net.BCrypt.HashPassword("dummy"), Role = "Admin", IsActive = true, Status = "Active" };
                _context.UserAccounts.Add(adminUser);
                await _context.SaveChangesAsync();
            }

            // ==========================================
            // 1. GENERATE 100 PROPERTIES & MEMBERSHIPS
            // ==========================================
            string[] firstNames = { "Juan", "Maria", "Jose", "Ana", "Miguel", "Elena", "Carlos", "Sofia", "Luis", "Carmen", "Paolo", "Diana", "Ramon", "Teresa", "Manuel" };
            string[] lastNames = { "Santos", "Reyes", "Cruz", "Bautista", "Ocampo", "Garcia", "Mendoza", "Torres", "Aquino", "Ramos", "Mercado", "Villanueva", "Castro", "Diaz", "Flores" };

            var newProperties = new List<Property>();

            for (int b = 1; b <= 10; b++)
            {
                for (int l = 1; l <= 10; l++)
                {
                    var prop = new Property
                    {
                        Block = b.ToString(),
                        Lot = l.ToString(),
                        Status = "Occupied",
                        DateRegistered = new DateOnly(startYear, 1, 1),
                        Memberships = new List<Membership>()
                    };

                    string fName = firstNames[rand.Next(firstNames.Length)];
                    string lName = lastNames[rand.Next(lastNames.Length)];

                    prop.Memberships.Add(new Membership
                    {
                        FirstName = fName,
                        LastName = lName,
                        ContactNumber = $"09{rand.Next(10000000, 99999999)}",
                        EmailAddress = $"{fName.ToLower()}.{lName.ToLower()}.b{b}l{l}@dummy-hoa.invalid",
                        CivilStatus = rand.Next(2) == 0 ? "Single" : "Married",
                        Sex = rand.Next(2) == 0 ? "Male" : "Female",
                        IsVoter = true,
                        YearsStay = rand.Next(1, 10),
                        TotalMembers = rand.Next(2, 6)
                    });

                    newProperties.Add(prop);
                }
            }

            _context.Properties.AddRange(newProperties);
            await _context.SaveChangesAsync();
            propsCreated = newProperties.Count; membersCreated = newProperties.Count;

            var propB1L1 = newProperties.First(p => p.Block == "1" && p.Lot == "1");
            var propB1L2 = newProperties.First(p => p.Block == "1" && p.Lot == "2");
            var otherProps = newProperties.Where(p => p.PropertyId != propB1L1.PropertyId && p.PropertyId != propB1L2.PropertyId).OrderBy(x => rand.Next()).ToList();

            // GENERATE 10 TENANTS (Safe past dates)
            for (int i = 0; i < 10; i++)
            {
                var p = otherProps[i];
                DateTime tStart = new DateTime(2022, 1, 1).AddDays(rand.Next((today - new DateTime(2022, 1, 1)).Days));
                _context.Tenants.Add(new Tenant { PropertyId = p.PropertyId, FullName = $"{firstNames[rand.Next(firstNames.Length)]} {lastNames[rand.Next(lastNames.Length)]}", ContactNumber = $"09{rand.Next(10000000, 99999999)}", Status = "Active", StartDate = tStart });
                tenantsCreated++;
            }
            await _context.SaveChangesAsync();

            // ==========================================
            // 2. PREPARE TRANSACTIONS (Certs, Vehicles, Dues)
            // ==========================================
            var rateSetting = await _context.AppSettings.FindAsync("MonthlyFee");
            decimal monthlyFee = rateSetting != null && decimal.TryParse(rateSetting.Value, out decimal pRate) ? pRate : 500m;

            var newCerts = new List<Certification>();
            var newVehicles = new List<Vehicle>();
            var newDues = new List<MonthlyDue>();
            var pendingTransactions = new List<Transaction>();

            // ------------------------------------------
            // A. CERTIFICATIONS (40 Total, spanning 2022-2026 safely)
            // ------------------------------------------
            string[] certTypes = { "Barangay Clearance", "Construction Permit", "Certificate Residency" };
            var certProps = new List<Property> { propB1L1, propB1L2 };
            certProps.AddRange(otherProps.Take(38));

            for (int i = 0; i < certProps.Count; i++)
            {
                var p = certProps[i];

                // Ensure dates are strictly <= today
                DateTime certDate;
                if (i < 5) // Force one for each year (2022-2026)
                {
                    int cYear = 2022 + i;
                    int maxDays = (cYear == currentYear) ? (today - new DateTime(currentYear, 1, 1)).Days : 365;
                    certDate = new DateTime(cYear, 1, 1).AddDays(rand.Next(Math.Max(1, maxDays)));
                }
                else
                {
                    certDate = new DateTime(2022, 1, 1).AddDays(rand.Next((today - new DateTime(2022, 1, 1)).Days));
                }

                string cType = certTypes[rand.Next(certTypes.Length)];
                string reqName = p.Memberships.First().LastName;

                newCerts.Add(new Certification { PropertyId = p.PropertyId, RequestorName = reqName, CertType = cType, DateIssued = DateOnly.FromDateTime(certDate), FeeAmount = 150.00m });
                pendingTransactions.Add(new Transaction { PropertyId = p.PropertyId, Amount = 150.00m, PaymentDate = DateOnly.FromDateTime(certDate), PaymentType = "Certification", EncodedBy = adminUser.UserId, Notes = $"{cType} - Requestor: {reqName}" });
                certsCreated++;
            }

            // ------------------------------------------
            // B. VEHICLES (50 Total - Strictly Past Dates)
            // ------------------------------------------
            string[] makes = { "Toyota", "Honda", "Mitsubishi", "Ford", "Nissan" };
            string[] models = { "Sedan", "SUV", "Hatchback", "Pickup" };
            string[] types = { "Car", "SUV", "Motorcycle" };

            int stickerCounter = 1;
            string b1l1LastName = propB1L1.Memberships.First().LastName;

            // B1-L1: Car 1 (Paid in 2025 and Jan/Feb 2026)
            newVehicles.Add(new Vehicle { PropertyId = propB1L1.PropertyId, VehicleOwner = b1l1LastName, VehicleType = "SUV", Make = "Honda", Model = "CRV", PlateNumber = "ABC-1111", StickerYear = 2025, StickerNumber = $"H-{stickerCounter++:D3}-25" });
            pendingTransactions.Add(new Transaction { PropertyId = propB1L1.PropertyId, Amount = 200.00m, PaymentDate = new DateOnly(2025, rand.Next(1, 12), rand.Next(1, 28)), PaymentType = "Sticker", EncodedBy = adminUser.UserId, Notes = "Vehicle Sticker Payment" });

            newVehicles.Add(new Vehicle { PropertyId = propB1L1.PropertyId, VehicleOwner = b1l1LastName, VehicleType = "SUV", Make = "Honda", Model = "CRV", PlateNumber = "ABC-1111", StickerYear = 2026, StickerNumber = $"H-{stickerCounter++:D3}-26" });
            pendingTransactions.Add(new Transaction { PropertyId = propB1L1.PropertyId, Amount = 200.00m, PaymentDate = new DateOnly(2026, rand.Next(1, 3), 1), PaymentType = "Sticker", EncodedBy = adminUser.UserId, Notes = "Vehicle Sticker Payment" }); // Forced to 1st of Jan/Feb to avoid future

            // B1-L1: Car 2
            newVehicles.Add(new Vehicle { PropertyId = propB1L1.PropertyId, VehicleOwner = b1l1LastName, VehicleType = "Car", Make = "Toyota", Model = "Vios", PlateNumber = "XYZ-9999", StickerYear = 2026, StickerNumber = $"H-{stickerCounter++:D3}-26" });
            pendingTransactions.Add(new Transaction { PropertyId = propB1L1.PropertyId, Amount = 200.00m, PaymentDate = new DateOnly(2026, rand.Next(1, 3), 1), PaymentType = "Sticker", EncodedBy = adminUser.UserId, Notes = "Vehicle Sticker Payment" });
            vehiclesCreated += 3;

            // B1-L2 gets 1 car
            string b1l2LastName = propB1L2.Memberships.First().LastName;
            newVehicles.Add(new Vehicle { PropertyId = propB1L2.PropertyId, VehicleOwner = b1l2LastName, VehicleType = "SUV", Make = "Ford", Model = "Everest", PlateNumber = "DEF-2222", StickerYear = 2026, StickerNumber = $"H-{stickerCounter++:D3}-26" });
            pendingTransactions.Add(new Transaction { PropertyId = propB1L2.PropertyId, Amount = 200.00m, PaymentDate = new DateOnly(2026, rand.Next(1, 3), 1), PaymentType = "Sticker", EncodedBy = adminUser.UserId, Notes = "Vehicle Sticker Payment" });
            vehiclesCreated++;

            // Remaining 46 Vehicles
            var vehProps = otherProps.Take(46).ToList();
            foreach (var p in vehProps)
            {
                // Generate safe past date
                DateTime vDate = new DateTime(2023, 1, 1).AddDays(rand.Next((today - new DateTime(2023, 1, 1)).Days));

                newVehicles.Add(new Vehicle { PropertyId = p.PropertyId, VehicleOwner = p.Memberships.First().LastName, VehicleType = types[rand.Next(types.Length)], Make = makes[rand.Next(makes.Length)], Model = models[rand.Next(models.Length)], PlateNumber = $"RND-{rand.Next(1000, 9999)}", StickerYear = vDate.Year, StickerNumber = $"H-{stickerCounter++:D3}-{(vDate.Year % 100):D2}" });
                pendingTransactions.Add(new Transaction { PropertyId = p.PropertyId, Amount = 200.00m, PaymentDate = DateOnly.FromDateTime(vDate), PaymentType = "Sticker", EncodedBy = adminUser.UserId, Notes = "Vehicle Sticker Payment" });
                vehiclesCreated++;
            }

            // ------------------------------------------
            // C. MONTHLY DUES (Strictly bounded to Today)
            // ------------------------------------------
            foreach (var prop in newProperties)
            {
                int profile;
                if (prop.PropertyId == propB1L1.PropertyId) profile = 20; // 11-30 is Advanced Payer
                else if (prop.PropertyId == propB1L2.PropertyId) profile = 95; // 86-100 is Late Payer
                else profile = rand.Next(1, 101);

                for (int year = startYear; year <= currentYear; year++)
                {
                    bool isYearlyAdvanced = (profile > 10 && profile <= 30);
                    int endMonth = (year == currentYear && !isYearlyAdvanced) ? currentMonth : 12;

                    if (isYearlyAdvanced)
                    {
                        // Yearly Payer logic: Pays lump sum in January
                        DateTime yearlyPaymentDate = new DateTime(year, 1, rand.Next(2, 15));

                        // If somehow we generated a future January date, pull it back to today
                        if (yearlyPaymentDate > today) yearlyPaymentDate = today;

                        for (int month = 1; month <= endMonth; month++)
                        {
                            newDues.Add(new MonthlyDue { PropertyId = prop.PropertyId, Month = month, Year = year, AmountDue = monthlyFee, TotalPaid = monthlyFee, Status = "Paid", DateFullyPaid = DateOnly.FromDateTime(yearlyPaymentDate) });
                        }

                        // One lump sum transaction
                        pendingTransactions.Add(new Transaction
                        {
                            PropertyId = prop.PropertyId,
                            Amount = monthlyFee * endMonth,
                            PaymentDate = DateOnly.FromDateTime(yearlyPaymentDate),
                            PaymentType = "MonthlyDue",
                            EncodedBy = adminUser.UserId,
                            Notes = $"Monthly Dues: 1/{year} to {endMonth}/{year} (Adv)"
                        });
                    }
                    else
                    {
                        // Standard Payers (On-Time, Late, Delinquent)
                        for (int month = 1; month <= endMonth; month++)
                        {
                            var due = new MonthlyDue { PropertyId = prop.PropertyId, Month = month, Year = year, AmountDue = monthlyFee, TotalPaid = 0, Status = "Unpaid" };

                            DateTime dueCreationDate = new DateTime(year, month, 1);
                            DateTime? paymentDate = null;
                            string note = $"Monthly Dues: {month}/{year}";

                            if (profile <= 10)
                            {
                                // Delinquent: Do nothing
                            }
                            else if (profile <= 85)
                            {
                                paymentDate = dueCreationDate.AddDays(rand.Next(0, 15)); // On-Time
                            }
                            else
                            {
                                paymentDate = dueCreationDate.AddDays(rand.Next(30, 90)); // Late
                            }

                            // ONLY process if the calculated date is literally on or before TODAY
                            if (paymentDate.HasValue && paymentDate.Value <= today)
                            {
                                due.Status = "Paid";
                                due.TotalPaid = monthlyFee;
                                due.DateFullyPaid = DateOnly.FromDateTime(paymentDate.Value);

                                pendingTransactions.Add(new Transaction
                                {
                                    PropertyId = prop.PropertyId,
                                    Amount = monthlyFee,
                                    PaymentDate = DateOnly.FromDateTime(paymentDate.Value),
                                    PaymentType = "MonthlyDue",
                                    EncodedBy = adminUser.UserId,
                                    Notes = note
                                });
                            }

                            newDues.Add(due);
                        }
                    }
                }
            }

            // ==========================================
            // 3. SORT, OR NUMBER ASSIGNMENT & DB SAVING
            // ==========================================
            pendingTransactions = pendingTransactions.OrderBy(x => x.PaymentDate).ToList();

            int orCounter = 1;
            foreach (var trans in pendingTransactions)
            {
                trans.OrNumber = orCounter.ToString("D4");
                orCounter++;
            }

            _context.Certifications.AddRange(newCerts);
            _context.Vehicles.AddRange(newVehicles);
            _context.MonthlyDues.AddRange(newDues);
            _context.Transactions.AddRange(pendingTransactions);
            await _context.SaveChangesAsync();

            duesCreated = newDues.Count;
            transCreated = pendingTransactions.Count;

            // ==========================================
            // 4. GENERATE AUDIT LOGS (Strictly Capped to Now)
            // ==========================================
            var newAuditLogs = new List<AuditLog>();

            foreach (var trans in pendingTransactions)
            {
                // Calculate an audit time based on payment date + random hours
                DateTime auditTime = trans.PaymentDate.ToDateTime(TimeOnly.MinValue).AddHours(rand.Next(8, 17));

                // CRITICAL FIX: Ensure AuditLog timestamp never exceeds the EXACT current time (now)
                if (auditTime > now) auditTime = now.AddMinutes(-rand.Next(1, 60));

                newAuditLogs.Add(new AuditLog
                {
                    ActionType = "Add",
                    TableName = "Transactions",
                    RecordId = trans.TransactionId,
                    UserId = adminUser.UserId,
                    Timestamp = auditTime,
                    Details = $"Collected ₱{trans.Amount:N2} ({trans.PaymentType}) from ID {trans.PropertyId}. OR#: {trans.OrNumber}."
                });
            }

            _context.AuditLogs.AddRange(newAuditLogs);
            await _context.SaveChangesAsync();

            return Content($@"
                SUCCESSFULLY SEEDED DATABASE!
                -----------------------------
                Properties Created: {propsCreated} (B1-L1 Adv, B1-L2 Late)
                Members Created: {membersCreated} (@dummy-hoa.invalid)
                Tenants Assigned: {tenantsCreated}
                Certificates Issued: {certsCreated}
                Vehicles Registered: {vehiclesCreated}
                -----------------------------
                Monthly Bills Created: {duesCreated}
                Transactions Created: {transCreated} (Safely restricted to Past/Present ONLY)
                Audit Logs Created: {newAuditLogs.Count} (No Future Timestamps)
            ");
        }
    }
}