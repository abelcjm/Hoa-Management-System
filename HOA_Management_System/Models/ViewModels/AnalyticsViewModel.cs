using HOA_Management_System.Models;

namespace HOA_Management_System.Models.ViewModels
{
    public class AnalyticsViewModel
    {
        public string FilterType { get; set; } = "Yearly";
        public int SelectedYear { get; set; }
        public int? SelectedPeriod { get; set; }
        public List<int> AvailableYears { get; set; }

        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal AverageEfficiency { get; set; }
        public decimal AverageDelinquencyRate { get; set; }

        public List<string> ChartLabels { get; set; }
        public List<decimal> CollectionData { get; set; }
        public List<decimal> TargetData { get; set; }
        public List<decimal?> DelinquencyData { get; set; }

        public List<string> BarColors { get; set; }

        public int CountPaid { get; set; }
        public int CountPartial { get; set; }
        public int CountUnpaid { get; set; }

        public List<decimal> CumulativeActual { get; set; }
        public List<decimal> CumulativeProjected { get; set; }

        public List<BlockHeatmapDto> BlockHeatmap { get; set; }

        public class BlockHeatmapDto
        {
            public string Block { get; set; }
            public int PropertyCount { get; set; }
            public int DelinquentCount { get; set; }
            public decimal DelinquencyRate { get; set; }
            public decimal TotalDebt { get; set; }
        }

        public decimal AmountDues { get; set; }
        public decimal AmountCertificates { get; set; }
        public decimal AmountStickers { get; set; }

        public decimal TotalExpectedDues { get; set; }

        public List<string> SnapshotLabels { get; set; }
        public List<decimal?> SnapshotData { get; set; }

        public List<OverdueAccountDto> TopDelinquents { get; set; }
        public List<string> GeneratedInsights { get; set; }
    }
}