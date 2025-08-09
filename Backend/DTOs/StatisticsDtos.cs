namespace Backend.DTOs
{
    /// <summary>
    /// Chứa các thông số thống kê tổng quan toàn hệ thống.
    /// </summary>
    public class GlobalStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalEnergyConsumedKWh { get; set; }
        public int TotalChargingSessions { get; set; }
        public int TotalStations { get; set; }
        public int TotalUsers { get; set; }
    }

    /// <summary>
    /// Đại diện cho một điểm dữ liệu trên biểu đồ theo thời gian.
    /// </summary>
    public class TimeSeriesDataPointDto
    {
        public string Date { get; set; }
        public decimal Value { get; set; }
    }

    /// <summary>
    /// Chứa thông tin thống kê sử dụng của một trạm sạc cụ thể.
    /// </summary>
    public class StationUsageDto
    {
        public int StationId { get; set; }
        public string StationName { get; set; }
        public int SessionCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalEnergyConsumed { get; set; }
    }
}
