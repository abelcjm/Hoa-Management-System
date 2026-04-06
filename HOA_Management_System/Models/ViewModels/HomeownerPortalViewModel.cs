using HOA_Management_System.Models;

namespace HOA_Management_System.Models.ViewModels
{
    public class HomeownerPortalViewModel
    {
        // Header Info
        public string FullName { get; set; }
        public string BlockLot { get; set; }
        public string Email { get; set; } // New field for UI
        public decimal TotalBalance { get; set; }
        public int UnpaidCount => UnpaidBills?.Count ?? 0;

        // Data Lists
        public List<MonthlyDue> UnpaidBills { get; set; }
        public List<Transaction> PaymentHistory { get; set; }
    }
}