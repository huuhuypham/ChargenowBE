using Backend.Data;
using Backend.DTOs;
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

        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalStats()
        {
            var totalRevenue = await _context.ChargingSessions.SumAsync(s => s.TotalCost);
            var totalEnergy = await _context.ChargingSessions.SumAsync(s => s.EnergyConsumedKWh);
            var totalSessions = await _context.ChargingSessions.CountAsync();
            var totalStations = await _context.ChargingStations.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            var stats = new GlobalStatsDto
            {
                TotalRevenue = totalRevenue,
                TotalEnergyConsumedKWh = totalEnergy,
                TotalChargingSessions = totalSessions,
                TotalStations = totalStations,
                TotalUsers = totalUsers
            };

            return Ok(stats);
        }

        // === PHẦN ĐÃ ĐƯỢC SỬA LỖI ===
        [HttpGet("revenue-over-time")]
        public async Task<IActionResult> GetRevenueOverTime([FromQuery] int days = 30)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            // 1. Lấy dữ liệu thô từ database, giữ nguyên kiểu DateTime
            var revenueDataRaw = await _context.ChargingSessions
                .Where(s => s.StartTime.Date >= startDate)
                .GroupBy(s => s.StartTime.Date)
                .Select(g => new
                {
                    Date = g.Key, // Giữ nguyên là DateTime
                    Value = g.Sum(s => s.TotalCost)
                })
                .OrderBy(d => d.Date)
                .ToListAsync(); // <-- Thực thi câu lệnh SQL ở đây

            // 2. Định dạng lại ngày tháng trong bộ nhớ của ứng dụng
            var formattedData = revenueDataRaw.Select(d => new TimeSeriesDataPointDto
            {
                Date = d.Date.ToString("yyyy-MM-dd"), // Thao tác này giờ an toàn
                Value = d.Value
            }).ToList();

            return Ok(formattedData);
        }

        [HttpGet("top-stations")]
        public async Task<IActionResult> GetTopStationsByRevenue([FromQuery] int count = 5)
        {
            var topStations = await _context.ChargingSessions
                .GroupBy(s => new {
                    s.Connector.ChargingStation.Id,
                    s.Connector.ChargingStation.Name
                })
                .Select(g => new StationUsageDto
                {
                    StationId = g.Key.Id,
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
