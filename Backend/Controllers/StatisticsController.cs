using Backend.Data;
using Backend.DTOs;
using Backend.Model; // Thêm using để nhận diện Enum ApprovalStatus
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [Route("api/statistics")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        // === ĐÃ SỬA: Lấy thống kê cho người dùng (ID=2) VÀ trạm đã được duyệt ===
        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalStats()
        {
            var ownerId = 1;

            var userStationsQuery = _context.ChargingStations
                .Where(s => s.OwnerId == ownerId && s.ApprovalStatus == ApprovalStatus.Approved);

            var totalRevenue = await _context.ChargingSessions
                .Where(s => s.Connector.ChargingStation.OwnerId == ownerId && s.Connector.ChargingStation.ApprovalStatus == ApprovalStatus.Approved)
                .SumAsync(s => s.TotalCost);

            var totalEnergy = await _context.ChargingSessions
                .Where(s => s.Connector.ChargingStation.OwnerId == ownerId && s.Connector.ChargingStation.ApprovalStatus == ApprovalStatus.Approved)
                .SumAsync(s => s.EnergyConsumedKWh);

            var totalSessions = await _context.ChargingSessions
                .Where(s => s.Connector.ChargingStation.OwnerId == ownerId && s.Connector.ChargingStation.ApprovalStatus == ApprovalStatus.Approved)
                .CountAsync();

            var totalStations = await userStationsQuery.CountAsync();

            var stats = new GlobalStatsDto
            {
                TotalRevenue = totalRevenue,
                TotalEnergyConsumedKWh = totalEnergy,
                TotalChargingSessions = totalSessions,
                TotalStations = totalStations,
            };

            return Ok(stats);
        }

        // === ĐÃ SỬA: Lấy doanh thu của người dùng (ID=2) VÀ trạm đã được duyệt ===
        [HttpGet("revenue-over-time")]
        public async Task<IActionResult> GetRevenueOverTime([FromQuery] int days = 30)
        {
            var ownerId = 1;
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var revenueDataRaw = await _context.ChargingSessions
                .Where(s => s.Connector.ChargingStation.OwnerId == ownerId
                            && s.Connector.ChargingStation.ApprovalStatus == ApprovalStatus.Approved // Thêm điều kiện lọc trạng thái
                            && s.StartTime.Date >= startDate)
                .GroupBy(s => s.StartTime.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Value = g.Sum(s => s.TotalCost)
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            var formattedData = revenueDataRaw.Select(d => new TimeSeriesDataPointDto
            {
                Date = d.Date.ToString("yyyy-MM-dd"),
                Value = d.Value
            }).ToList();

            return Ok(formattedData);
        }

        // === ĐÃ SỬA: Lấy top trạm sạc đã được duyệt trên toàn hệ thống ===
        [HttpGet("top-stations")]
        public async Task<IActionResult> GetTopStationsByRevenue([FromQuery] int count = 5)
        {
            var topStations = await _context.ChargingSessions
                .Where(s => s.Connector.ChargingStation.ApprovalStatus == ApprovalStatus.Approved) // Thêm điều kiện lọc trạng thái
                .GroupBy(s => new {
                    s.Connector.ChargingStation.Id,
                    s.Connector.ChargingStation.Name
                })
                .Select(g => new StationUsageDto
                {
                    StationId = 1,
                    StationName = g.Key.Name,
                    SessionCount = g.Count(),
                    TotalRevenue = g.Sum(s => s.TotalCost),
                    TotalEnergyConsumed = g.Sum(s => s.EnergyConsumedKWh)
                })
                .OrderByDescending(s => s.TotalRevenue)
                .Take(count)
                .ToListAsync();

            return Ok(topStations);
        }
    }
}
