using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class TenantDashboardViewModel
    {
        public bool HasActiveLease { get; set; }
        public string? PropertyName { get; set; }
        public string? UnitName { get; set; }
        public decimal MonthlyRent { get; set; }

        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int OverdueInvoiceCount { get; set; }
        public RentInvoice? NextDueInvoice { get; set; }

        public List<MonthlyCollectionPoint> MonthlyPaymentHistory { get; set; } = new();
        public List<RentInvoice> Invoices { get; set; } = new();

        public decimal UtilityOutstandingBalance { get; set; }
        public List<UtilityBillShare> UtilityShares { get; set; } = new();
    }
}
