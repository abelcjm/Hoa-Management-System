using HOA_Management_System.Models;

namespace HOA_Management_System.Models.ViewModels
{
    public class EncoderDashboardViewModel
    {
        // 1. CARDS DATA
        public decimal TotalCollectedThisMonth { get; set; }
        public decimal TotalOutstandingBalance { get; set; }
        public int OverdueAccountsCount { get; set; } // Distinct properties with past due bills
        public decimal CollectedToday { get; set; }
        public int UnpaidAccountsThisMonthCount { get; set; }

        // 2. RECENT TRANSACTIONS (Left Table)
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();

        // 3. TOP OVERDUE ACCOUNTS (Right List)
        public List<OverdueAccountDto> TopOverdueAccounts { get; set; } = new();

        // 4. DAILY TREND CHART (Bottom Chart)
        public List<string> DailyTrendLabels { get; set; } = new();
        public List<decimal> DailyTrendValues { get; set; } = new();
    }

    // Helper DTO for the Transaction Table
    public class RecentTransactionDto
    {
        public string Date { get; set; }
        public string Homeowner { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string OrNumber { get; set; }
    }
}