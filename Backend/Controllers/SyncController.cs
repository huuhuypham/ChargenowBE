using Backend.Data;
using Backend.Model;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly IGooglePlacesService _googlePlacesService;
        private readonly AppDbContext _context;
        private readonly ILogger<SyncController> _logger;

        public SyncController(IGooglePlacesService googlePlacesService, AppDbContext context, ILogger<SyncController> logger)
        {
            _googlePlacesService = googlePlacesService;
            _context = context;
            _logger = logger;
        }

        public class SyncRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        [HttpPost("stations")]
        public async Task<IActionResult> SyncStations([FromBody] SyncRequest request)
        {
            try
            {
                var placeIds = await _googlePlacesService.GetNearbyStationIds(request.Latitude, request.Longitude);

                if (placeIds == null || !placeIds.Any())
                {
                    return Ok("No charging stations found nearby.");
                }

                int newStations = 0;
                int updatedStations = 0;

                foreach (var placeId in placeIds)
                {
                    var details = await _googlePlacesService.GetStationDetails(placeId);

                    if (details?.DisplayName == null || details.Location == null)
                    {
                        _logger.LogWarning("Skipping place with ID {PlaceId} due to missing essential details.", placeId);
                        continue;
                    }

                    var existingStation = await _context.ChargingStations
                        .FirstOrDefaultAsync(s => s.GooglePlaceId == placeId);

                    string operatingHours = "N/A";
                    if (details.OpeningHours?.WeekdayDescriptions != null && details.OpeningHours.WeekdayDescriptions.Any())
                    {
                        operatingHours = string.Join("; ", details.OpeningHours.WeekdayDescriptions);
                    }

                    if (existingStation != null)
                    {
                        existingStation.Name = details.DisplayName.Text;
                        existingStation.Address = details.FormattedAddress ?? "N/A";
                        existingStation.Latitude = details.Location.Latitude;
                        existingStation.Longitude = details.Location.Longitude;
                        existingStation.OperatingHours = operatingHours;
                        updatedStations++;
                    }
                    else
                    {
                        var newStation = new ChargingStation
                        {
                            GooglePlaceId = details.Id,
                            Name = details.DisplayName.Text,
                            Address = details.FormattedAddress ?? "N/A",
                            Latitude = details.Location.Latitude,
                            Longitude = details.Location.Longitude,
                            OperatingHours = operatingHours,
                            ApprovalStatus = ApprovalStatus.Approved,
                            OwnerId = 1
                        };
                        _context.ChargingStations.Add(newStation);
                        newStations++;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok($"Sync completed. Added: {newStations}, Updated: {updatedStations}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during station synchronization.");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}