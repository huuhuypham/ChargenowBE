namespace Backend.DTOs
{
    public class NearbySearchResponse { public List<PlaceResult> Places { get; set; } }
    public class PlaceResult { public string Id { get; set; } }
    public class PlaceDetailsResponse { public string Id { get; set; } public DisplayName DisplayName { get; set; } public string FormattedAddress { get; set; } public Location Location { get; set; } public double? Rating { get; set; } public RegularOpeningHours OpeningHours { get; set; } }
    public class DisplayName { public string Text { get; set; } }
    public class Location { public double Latitude { get; set; } public double Longitude { get; set; } }
    public class RegularOpeningHours { public List<string> WeekdayDescriptions { get; set; } }
}
