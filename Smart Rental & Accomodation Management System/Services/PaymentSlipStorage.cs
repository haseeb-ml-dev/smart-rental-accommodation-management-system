namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class PaymentSlipStorage
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["application/pdf"] = ".pdf"
        };

        private readonly IWebHostEnvironment _env;

        public PaymentSlipStorage(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Trusts only the sniffed content type for the extension — never the client-supplied file name.
        public bool IsAllowed(IFormFile file, out string extension)
        {
            if (file.Length > 0 && file.Length <= MaxFileSizeBytes && AllowedContentTypes.TryGetValue(file.ContentType, out var ext))
            {
                extension = ext;
                return true;
            }

            extension = string.Empty;
            return false;
        }

        // category is "rent" or "utility"; entityId is the RentInvoice.Id or UtilityBillShare.Id.
        public async Task<string> SaveAsync(string category, int entityId, IFormFile file, string extension)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "payment-slips", category, entityId.ToString());
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);

            return fileName;
        }
    }
}
