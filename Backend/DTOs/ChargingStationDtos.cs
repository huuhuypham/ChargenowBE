using System.ComponentModel.DataAnnotations;
using Backend.Model; // Namespace chứa các enum của bạn

namespace Backend.DTOs
{
    // --- DTOs cho Connector ---

    public class ConnectorDto
    {
        public int Id { get; set; }
        public string ConnectorType { get; set; }
        public double MaxPowerKW { get; set; }
        public string Status { get; set; } // Trả về dạng chuỗi cho dễ đọc
    }

    public class CreateUpdateConnectorDto
    {
        [Required]
        public string ConnectorType { get; set; }

        [Required]
        [Range(1, 1000)]
        public double MaxPowerKW { get; set; }

        [Required]
        public ChargingStationStatus Status { get; set; }
    }


    // --- DTOs cho ChargingStation ---

    public class ChargingStationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? OperatingHours { get; set; }
        public string ApprovalStatus { get; set; }
        public int OwnerId { get; set; }
        public List<ConnectorDto> Connectors { get; set; } = new List<ConnectorDto>();
    }

    public class CreateUpdateChargingStationDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }

        public string? OperatingHours { get; set; }

        // OwnerId sẽ được lấy từ token của người dùng đang đăng nhập
        // ApprovalStatus sẽ được đặt là Pending theo mặc định
    }
}
public class StationSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public int TotalConnectors { get; set; }
    public int AvailableConnectors { get; set; }
    public string ApprovalStatus { get; set; }
}