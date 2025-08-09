// In Backend/DTOs/StationResponseDto.cs
namespace Backend.DTOs
{
    // DTO cho thông tin của một cổng sạc
    public class ConnectorResponseDto
    {
        public int Id { get; set; }
        public string ConnectorType { get; set; }
        public double MaxPowerKW { get; set; }
        public string Status { get; set; }
        // Lưu ý: Không có thuộc tính ChargingStation ở đây để phá vỡ vòng lặp
    }

    // DTO cho thông tin đầy đủ của một trạm sạc
    public class StationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string? OperatingHours { get; set; }
        public string ApprovalStatus { get; set; }
        public int OwnerId { get; set; }
        public List<ConnectorResponseDto> Connectors { get; set; } = new List<ConnectorResponseDto>();
    }
}