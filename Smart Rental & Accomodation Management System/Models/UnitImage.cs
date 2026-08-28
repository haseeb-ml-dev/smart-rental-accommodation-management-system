namespace Smart_Rental___Accomodation_Management_System.Models
{
    public class UnitImage
    {
        public int Id { get; set; }

        public int UnitId { get; set; }
        public Unit? Unit { get; set; }

        // GUID-based name of the file on disk under wwwroot/uploads/units/{UnitId}/ — never the client's original file name.
        public string FileName { get; set; } = string.Empty;

        public bool IsCover { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
