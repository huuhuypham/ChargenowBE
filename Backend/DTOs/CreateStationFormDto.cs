// In: Backend/DTOs/CreateStationFormDto.cs
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class CreateStationFormDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        public string? OperatingHours { get; set; }

        // Nhận chuỗi JSON chứa thông tin các cổng sạc
        [Required]
        public string Connectors { get; set; }

        // Dù chưa xử lý, vẫn cần khai báo để nhận file từ form
        public IFormFileCollection? StationImages { get; set; }
        public IFormFile? BusinessLicense { get; set; }
    }

    // DTO phụ để deserialize chuỗi JSON của connector
    public class ConnectorInfo
    {
        public string ConnectorType { get; set; }
        public double MaxPowerKW { get; set; }
        public decimal Price { get; set; }
    }
}