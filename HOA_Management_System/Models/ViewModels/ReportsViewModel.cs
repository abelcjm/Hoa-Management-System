using HOA_Management_System.Models;

namespace HOA_Management_System.Models.ViewModels
{
    public class ReportsViewModel
    {
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; }

        // Filters
        public string ReportType { get; set; }
        public string ExportFormat { get; set; }
        public string FilterType { get; set; }
        public int? SelectedPeriod { get; set; }

        // --- FINANCIAL METRICS ---
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalExpectedDues { get; set; }
        public decimal EfficiencyRate { get; set; }
        public decimal DelinquencyRate { get; set; }

        // --- CHART DATA ---
        public List<string> ChartLabels { get; set; }
        public List<decimal> CollectionData { get; set; }
        public List<decimal> TargetData { get; set; }
        public List<decimal?> DelinquencyData { get; set; }
        public List<decimal?> SnapshotData { get; set; } // Historical Compliance
        public List<string> BarColors { get; set; }
        public List<decimal> CumulativeActual { get; set; }
        public List<decimal> CumulativeProjected { get; set; }

        public decimal AmountDues { get; set; }
        public decimal AmountCerts { get; set; }
        public decimal AmountStickers { get; set; }

        public List<MonthlyPerformanceDto> MonthlyPerformance { get; set; }
        public List<ReportHeatmapDto> Heatmap { get; set; }
        public List<OverdueAccountDto> TopDelinquents { get; set; }
        public List<Transaction> RawTransactions { get; set; }

        // --- SECTIONAL INSIGHTS ---
        public string ExecutiveInsight { get; set; }
        public string TrendsInsight { get; set; }
        public string RevenueInsight { get; set; }
        public string DetailsInsight { get; set; }

        public class MonthlyPerformanceDto
        {
            public string MonthName { get; set; }
            public decimal Actual { get; set; }
            public decimal Projected { get; set; }
            public decimal CumulativeActual { get; set; }
            public decimal CumulativeProjected { get; set; }
        }

        public class ReportHeatmapDto
        {
            public string Block { get; set; }
            public int PropertyCount { get; set; }
            public int DelinquentCount { get; set; }
            public decimal DelinquencyRate { get; set; }
            public decimal TotalDebt { get; set; }
        }
    }
}