using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model
{
    public enum UserRole
    {
        User,       // Người dùng thông thường
        Agent,      // Đại lý (chủ trạm sạc)
        Maintenance, // Nhân viên bảo trì
        Admin       // Quản trị viên
    }

    public enum ChargingStationStatus
    {
        Available,  // Sẵn sàng
        Charging,   // Đang sạc
        Reserved,   // Đã được đặt trước
        Unavailable, // Không sẵn sàng (lỗi, bảo trì)
        Unknown     // Không rõ trạng thái
    }

    public enum ApprovalStatus
    {
        Pending,    // Chờ duyệt
        Approved,   // Đã duyệt
        Rejected    // Bị từ chối
    }

    public enum BookingStatus
    {
        Confirmed,  // Đã xác nhận
        Completed,  // Hoàn thành
        Cancelled,  // Đã hủy
        Expired     // Hết hạn
    }

    public enum PaymentStatus
    {
        Pending,    // Đang chờ
        Paid,       // Đã thanh toán
        Failed      // Thất bại
    }

    /// <summary>
    /// Đại diện cho tất cả các loại tài khoản trong hệ thống.
    /// Vai trò được xác định bởi thuộc tính Role.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; } // Luôn mã hóa mật khẩu
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public decimal Balance { get; set; } // Số dư tài khoản
        public UserRole Role { get; set; }

        // Navigation Properties - Các mối quan hệ
        public virtual ICollection<ChargingStation> OwnedStations { get; set; } = new List<ChargingStation>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<ChargingSession> ChargingSessions { get; set; } = new List<ChargingSession>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }

    /// <summary>
    /// Thông tin về một trạm sạc.
    /// </summary>
    public class ChargingStation
    {
        public int Id { get; set; }

        public string? GooglePlaceId { get; set; } // Dùng để kiểm tra trùng lặp

        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? OperatingHours { get; set; }

        // Chủ sở hữu (đại lý)
        public int OwnerId { get; set; }
        public virtual User Owner { get; set; } = null!;

        public ApprovalStatus ApprovalStatus { get; set; }

        // Giá mặc định (có thể bị override bởi rule/phân đoạn sau này)
        public decimal DefaultPricePerKWh { get; set; }

        // Trạng thái tổng quan của trạm (ví dụ: có đang vận hành bình thường không).
        public bool IsOperational { get; set; } = true;

        // Audit / soft delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        // Navigation
        public virtual ICollection<Connector> Connectors { get; set; } = new List<Connector>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();

        // Example computed helper (không map vào DB)
        [NotMapped]
        public int AvailableConnectorCount =>
            Connectors.Count(c => c.Status == ChargingStationStatus.Available); // nếu giữ tên enum như hiện tại
    }


    /// <summary>
    /// Thông tin về một cổng sạc cụ thể tại một trạm.
    /// Một trạm có thể có nhiều cổng sạc.
    /// </summary>
    public class Connector
    {
        public int Id { get; set; }
        public string ConnectorType { get; set; } // Loại cổng sạc, vd: "Type 2", "CCS2"
        public double MaxPowerKW { get; set; } // Công suất tối đa (kW)
        public ChargingStationStatus Status { get; set; }

        public int ChargingStationId { get; set; } // Foreign Key
        public virtual ChargingStation ChargingStation { get; set; }
    }

    /// <summary>
    /// Lịch sử một phiên sạc của người dùng.
    /// </summary>
    public class ChargingSession
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal EnergyConsumedKWh { get; set; } // Năng lượng tiêu thụ
        public decimal TotalCost { get; set; } // Tổng chi phí

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int ConnectorId { get; set; }
        public virtual Connector Connector { get; set; }

        // Thông tin thanh toán
        public string PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string TransactionId { get; set; } // ID giao dịch từ cổng thanh toán

        public int? VoucherId { get; set; } // Voucher có thể có hoặc không
        public virtual Voucher? Voucher { get; set; }
    }

    /// <summary>
    /// Đặt trước một cổng sạc tại một trạm.
    /// </summary>
    public class Booking
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int ConnectorId { get; set; }
        public virtual Connector Connector { get; set; }
    }

    /// <summary>
    /// Đánh giá của người dùng cho một trạm sạc.
    /// </summary>
    public class Review
    {
        public int Id { get; set; }
        public int Rating { get; set; } // Ví dụ: 1 đến 5 sao
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int ChargingStationId { get; set; }
        public virtual ChargingStation ChargingStation { get; set; }
    }

    /// <summary>
    /// Thông tin về voucher giảm giá.
    /// </summary>
    public class Voucher
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; } // Ngày hết hạn
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Nhật ký bảo trì cho một trạm sạc.
    /// </summary>
    public class MaintenanceLog
    {
        public int Id { get; set; }
        public string Reason { get; set; } // Lý do bảo trì
        public string Details { get; set; } // Chi tiết công việc đã làm
        public DateTime MaintenanceDate { get; set; }

        public int ChargingStationId { get; set; }
        public virtual ChargingStation ChargingStation { get; set; }

        public int? MaintenanceStaffId { get; set; } // ID của nhân viên bảo trì (User có Role=Maintenance)
        public virtual User? MaintenanceStaff { get; set; }
    }
}
