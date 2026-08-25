using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class MonthlyCollectionPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Collected { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class LandlordDashboardViewModel
    {
        public int TotalProperties { get; set; }
        public int TotalUnits { get; set; }
        public int OccupiedUnits { get; set; }
        public int VacantUnits { get; set; }
        public int ActiveTenants { get; set; }

        public decimal CollectedThisMonth { get; set; }
        public decimal OutstandingThisMonth { get; set; }
        public int OverdueInvoiceCount { get; set; }

        public List<MonthlyCollectionPoint> MonthlyCollection { get; set; } = new();
        public List<RentInvoice> RecentInvoices { get; set; } = new();

        public decimal OutstandingUtilities { get; set; }
    }
}
