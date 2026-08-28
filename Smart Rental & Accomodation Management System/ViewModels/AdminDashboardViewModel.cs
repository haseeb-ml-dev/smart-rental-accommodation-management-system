using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalLandlords { get; set; }
        public int TotalTenants { get; set; }
        public int TotalProperties { get; set; }
        public int TotalUnits { get; set; }

        public decimal CollectedThisMonth { get; set; }
        public decimal OutstandingThisMonth { get; set; }
        public int OverdueInvoiceCount { get; set; }

        public List<MonthlyCollectionPoint> MonthlyCollection { get; set; } = new();
        public List<Property> Properties { get; set; } = new();
        public List<RentInvoice> RecentInvoices { get; set; } = new();

        public string? TopRatedPropertyName { get; set; }
        public double? TopRatedPropertyAverage { get; set; }
    }
}
