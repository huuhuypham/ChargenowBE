using Backend.Model;

namespace Backend.DTOs
{
    public class ChargingSessionDto
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal EnergyConsumedKWh { get; set; }
        public decimal TotalCost { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        // SỬA LỖI: Sử dụng tên thuộc tính mới
        public string? PaymentGatewayTransactionId { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }

        public int ConnectorId { get; set; }
        public string ConnectorType { get; set; }

        public int StationId { get; set; }
        public string StationName { get; set; }

        public ChargingSessionDto(ChargingSession session)
        {
            Id = session.Id;
            StartTime = session.StartTime;
            EndTime = session.EndTime;
            EnergyConsumedKWh = session.EnergyConsumedKWh;
            TotalCost = session.TotalCost;
            PaymentStatus = session.PaymentStatus.ToString();
            PaymentMethod = session.PaymentMethod;
            // SỬA LỖI: Sử dụng tên thuộc tính mới
            PaymentGatewayTransactionId = session.PaymentGatewayTransactionId;
            UserId = session.UserId;
            UserName = session.User.FullName;
            ConnectorId = session.ConnectorId;
            ConnectorType = session.Connector.ConnectorType;
            StationId = session.Connector.ChargingStation.Id;
            StationName = session.Connector.ChargingStation.Name;
        }
    }

    /// <summary>
    /// DTO cho yêu cầu bắt đầu một phiên sạc.
    /// </summary>
    public class StartSessionDto
    {
        public int UserId { get; set; }
        public int ConnectorId { get; set; }
    }

    /// <summary>
    /// DTO cho yêu cầu kết thúc một phiên sạc.
    /// </summary>
    public class EndSessionDto
    {
        public decimal EnergyConsumedKWh { get; set; }
    }

    /// <summary>
    /// DTO chứa thông tin thống kê các phiên sạc của người dùng.
    /// </summary>
    public class UserChargingStatsDto
    {
        public int UserId { get; set; }
        public int TotalSessions { get; set; }
        public decimal TotalEnergyConsumedKWh { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// DTO cho yêu cầu bắt đầu một phiên sạc.
    /// </summary>
   
    /// <summary>
    /// DTO chứa thông tin thống kê các phiên sạc của người dùng.
    /// </summary>
    

}
