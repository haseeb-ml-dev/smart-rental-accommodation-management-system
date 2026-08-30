using System.Globalization;
using System.Text.Json;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    public record GeocodeResult(double Latitude, double Longitude);

    // Wraps OpenStreetMap's Nominatim geocoder — free and keyless, which suits a demo/small
    // deployment, but its usage policy caps requests at ~1/second and requires a descriptive
    // User-Agent (both set below). For higher volume, self-host Nominatim or switch to a paid
    // provider (Google Geocoding, LocationIQ, OpenCage). Every failure mode (network error, bad
    // address, no match, rate limiting) returns null rather than throwing — geocoding is a
    // nice-to-have enrichment and must never block saving a property.
    public class GeocodingService
    {
        private const string UserAgent = "SmartRentalAccommodationManagementSystem/1.0 (demo project)";

        private readonly HttpClient _httpClient;
        private readonly ILogger<GeocodingService> _logger;

        public GeocodingService(HttpClient httpClient, ILogger<GeocodingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<GeocodeResult?> GeocodeAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            try
            {
                var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd(UserAgent);

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var results = doc.RootElement;

                if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
                {
                    return null;
                }

                var first = results[0];
                var lat = double.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
                var lon = double.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
                return new GeocodeResult(lat, lon);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or FormatException)
            {
                _logger.LogWarning(ex, "Geocoding request for {Address} failed", address);
                return null;
            }
        }
    }
}
