namespace HOA_Management_System.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // 1. Overview Cards
        public int TotalHomeowners { get; set; }
        public decimal TotalCollectedThisMonth { get; set; }
        public decimal TotalOutstandingBalance { get; set; } // Global debt
        public decimal CollectionEfficiency { get; set; } // % (0-100)
        public decimal EfficiencyChange { get; set; } // e.g. +2.5 or -3.0

        // 2. Financial Health
        public string HealthStatus { get; set; } // "Healthy", "Warning", "Critical"
        public string InsightMessage { get; set; } // "Collection efficiency decreased..."

        // 3. Charts Data
        public List<string> TrendLabels { get; set; } // ["Jan", "Feb", ...]
        public List<decimal> TrendCollected { get; set; }
        public List<decimal> TrendTarget { get; set; }

        public int PiePaid { get; set; }
        public int PiePartial { get; set; }
        public int PieUnpaid { get; set; }
    }
}