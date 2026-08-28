namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class UnitImageStorage
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;
        public const int MaxImagesPerUnit = 10;

        private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

        private readonly IWebHostEnvironment _env;

        public UnitImageStorage(IWebHostEnvironment env)
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

        public async Task<string> SaveAsync(int unitId, IFormFile file, string extension)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "units", unitId.ToString());
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);

            return fileName;
        }

        public void Delete(int unitId, string fileName)
        {
            var fullPath = Path.Combine(_env.WebRootPath, "uploads", "units", unitId.ToString(), fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
