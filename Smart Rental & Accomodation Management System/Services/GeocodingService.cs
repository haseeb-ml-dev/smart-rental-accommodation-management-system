using System.Text.Json;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    public record GeocodeResult(double Latitude, double Longitude);

    // Wraps the Google Geocoding API. Every failure mode (no key configured, network error,
    // bad address, quota exceeded) returns null rather than throwing — geocoding is a nice-to-have
    // enrichment and must never block saving a property.
    public class GeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeocodingService> _logger;

        public GeocodingService(HttpClient httpClient, IConfiguration configuration, ILogger<GeocodingService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GeocodeResult?> GeocodeAsync(string address)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            try
            {
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={apiKey}";
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var root = doc.RootElement;

                var status = root.GetProperty("status").GetString();
                if (status != "OK" || root.GetProperty("results").GetArrayLength() == 0)
                {
                    if (status is not ("OK" or "ZERO_RESULTS"))
                    {
                        _logger.LogWarning("Geocoding request for {Address} returned status {Status}", address, status);
                    }
                    return null;
                }

                var location = root.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
                return new GeocodeResult(location.GetProperty("lat").GetDouble(), location.GetProperty("lng").GetDouble());
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Geocoding request for {Address} failed", address);
                return null;
            }
        }
    }
}
