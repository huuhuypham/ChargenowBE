using Backend.DTOs;
namespace Backend.Services
{
    public interface IGooglePlacesService
    {
        Task<List<string>> GetNearbyStationIds(double latitude, double longitude);
        Task<PlaceDetailsResponse> GetStationDetails(string placeId);
    }
}
