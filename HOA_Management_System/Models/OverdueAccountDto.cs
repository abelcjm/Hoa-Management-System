namespace HOA_Management_System.Models
{
    public class OverdueAccountDto
    {
        public int PropertyId { get; set; }
        public string Homeowner { get; set; }
        public string BlockLot { get; set; }
        public int MonthsOverdue { get; set; }
        public decimal TotalDebt { get; set; }
    }
}