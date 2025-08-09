using Backend.DTOs;
using System.Text.Json;

namespace Backend.Services
{
    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public GooglePlacesService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("GooglePlaces");
            _apiKey = configuration["GooglePlacesApiKey"] ?? throw new InvalidOperationException("Google API Key not configured.");
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<string>> GetNearbyStationIds(double latitude, double longitude)
        {
            var requestBody = new
            {
                includedTypes = new[] { "electric_vehicle_charging_station" },
                maxResultCount = 20,
                locationRestriction = new
                {
                    circle = new
                    {
                        center = new { latitude, longitude },
                        radius = 5000.0 // Bán kính 5km
                    }
                },
                rankPreference = "DISTANCE"
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/places:searchNearby");
            requestMessage.Content = content;
            requestMessage.Headers.Add("X-Goog-Api-Key", _apiKey);
            requestMessage.Headers.Add("X-Goog-FieldMask", "places.id");

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            var searchResult = await JsonSerializer.DeserializeAsync<NearbySearchResponse>(responseStream, _jsonOptions);

            return searchResult?.Places?.Select(p => p.Id).ToList() ?? new List<string>();
        }

        public async Task<PlaceDetailsResponse> GetStationDetails(string placeId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"v1/places/{placeId}");
            requestMessage.Headers.Add("X-Goog-Api-Key", _apiKey);
            requestMessage.Headers.Add("X-Goog-FieldMask", "id,displayName,formattedAddress,location,rating,regularOpeningHours");

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            var detailsResult = await JsonSerializer.DeserializeAsync<PlaceDetailsResponse>(responseStream, _jsonOptions);

            return detailsResult ?? throw new InvalidOperationException("Failed to deserialize place details.");
        }
    }
}
