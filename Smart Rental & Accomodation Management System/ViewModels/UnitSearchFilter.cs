using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    // Bound from the Browse/Index query string so search results stay linkable and paginate-able.
    public class UnitSearchFilter
    {
        public string? City { get; set; }
        public string? Area { get; set; }
        public decimal? MinRent { get; set; }
        public decimal? MaxRent { get; set; }
        public UnitType? UnitType { get; set; }
        public BhkType? BhkType { get; set; }
        public int Page { get; set; } = 1;
    }
}
