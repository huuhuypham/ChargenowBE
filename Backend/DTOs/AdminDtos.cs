using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    // DTO này chứa đầy đủ thông tin để hiển thị trên trang quản trị
    public class AdminStationViewDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string? OperatingHours { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin người sở hữu trạm
        public int OwnerId { get; set; }
        public string OwnerFullName { get; set; }
        public string OwnerEmail { get; set; }

        // Thông tin các cổng sạc để xem chi tiết
        public List<ConnectorInfo> Connectors { get; set; } = new List<ConnectorInfo>();
    }

    // DTO để cập nhật trạng thái phê duyệt
    public class UpdateApprovalStatusDto
    {
        [Required]
        // Nhận vào "Approved" hoặc "Rejected"
        public string NewStatus { get; set; }
        public string? RejectionReason { get; set; } // Lý do từ chối (tùy chọn)
    }
}