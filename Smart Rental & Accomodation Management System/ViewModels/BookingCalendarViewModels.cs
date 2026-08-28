using Smart_Rental___Accomodation_Management_System.Models;

namespace Smart_Rental___Accomodation_Management_System.ViewModels
{
    public class TimelineBarViewModel
    {
        public string TenantName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public decimal LeftPercent { get; set; }
        public decimal WidthPercent { get; set; }
    }

    public class TimelineMarkerViewModel
    {
        public string TenantName { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; }
        public decimal LeftPercent { get; set; }
    }

    public class MonthMarkerViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal LeftPercent { get; set; }
    }

    public class UnitTimelineViewModel
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public UnitType UnitType { get; set; }
        public List<TimelineBarViewModel> Bars { get; set; } = new();
        public List<TimelineMarkerViewModel> PendingMarkers { get; set; } = new();
    }

    public class BookingCalendarViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }
        public int MonthOffset { get; set; }
        public List<MonthMarkerViewModel> MonthMarkers { get; set; } = new();
        public List<UnitTimelineViewModel> Units { get; set; } = new();
    }
}
