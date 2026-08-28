using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class PropertyOccupancyRow
    {
        public string PropertyName { get; set; } = string.Empty;
        public int TotalUnits { get; set; }
        public int OccupiedUnits { get; set; }
        public double OccupancyRate => TotalUnits == 0 ? 0 : (double)OccupiedUnits / TotalUnits * 100;
    }

    public class InvoiceReportRow
    {
        public string PropertyName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public int PeriodYear { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime? PaidDate { get; set; }
    }

    public class LandlordReportData
    {
        public string LandlordName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public int TotalProperties { get; set; }
        public int TotalUnits { get; set; }
        public int OccupiedUnits { get; set; }
        public int VacantUnits { get; set; }
        public int ActiveTenants { get; set; }

        public decimal CollectedThisMonth { get; set; }
        public decimal OutstandingThisMonth { get; set; }
        public int OverdueInvoiceCount { get; set; }
        public decimal OutstandingUtilities { get; set; }

        public List<MonthlyCollectionPoint> MonthlyCollection { get; set; } = new();
        public List<PropertyOccupancyRow> PropertyOccupancy { get; set; } = new();
        public List<InvoiceReportRow> Invoices { get; set; } = new();
    }
}
