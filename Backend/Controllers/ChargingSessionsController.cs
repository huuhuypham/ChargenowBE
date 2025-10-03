using Backend.Data; // Dùng để lấy thông tin user đã xác thực
using Backend.Model; // Namespace chứa các model của bạn
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
//using Backend.DTOs;
// Giả sử bạn có một DbContext tên là ApplicationDbContext
// using Backend.Data;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChargingSessionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChargingSessionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ChargingSessions
        /// <summary>
        /// Lấy danh sách lịch sử các phiên sạc.
        /// Có thể lọc theo userId. Dành cho Admin hoặc chính user đó xem.
        /// </summary>
        /// <param name="userId">ID của người dùng cần xem lịch sử.</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChargingSessionDto>>> GetChargingSessions([FromQuery] int? userId)
        {
            var query = _context.ChargingSessions
                .Include(cs => cs.User)
                .Include(cs => cs.Connector)
                    .ThenInclude(c => c.ChargingStation)
                .AsQueryable();

            // Nếu có userId được cung cấp, lọc theo user đó
            if (userId.HasValue)
            {
                // TODO: Thêm logic kiểm tra quyền, ví dụ: chỉ admin hoặc chính user đó mới được xem
                query = query.Where(cs => cs.UserId == userId.Value);
            }

            var sessions = await query
                .OrderByDescending(cs => cs.StartTime)
                .Select(cs => new ChargingSessionDto(cs)) // Chuyển đổi sang DTO
                .ToListAsync();

            return Ok(sessions);
        }

        // GET: api/ChargingSessions/5
        /// <summary>
        /// Lấy thông tin chi tiết của một phiên sạc theo ID.
        /// </summary>
        /// <param name="id">ID của phiên sạc.</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<ChargingSessionDto>> GetChargingSession(int id)
        {
            var session = await _context.ChargingSessions
                .Include(cs => cs.User)
                .Include(cs => cs.Connector)
                    .ThenInclude(c => c.ChargingStation)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (session == null)
            {
                return NotFound($"Không tìm thấy phiên sạc với ID {id}.");
            }

            // TODO: Thêm logic kiểm tra quyền

            return Ok(new ChargingSessionDto(session));
        }

        // GET: api/ChargingSessions/user/21/stats
        /// <summary>
        /// Lấy thống kê các phiên sạc của một người dùng.
        /// </summary>
        /// <param name="userId">ID của người dùng.</param>
        [HttpGet("user/{userId}/stats")]
        public async Task<ActionResult<UserChargingStatsDto>> GetUserChargingStats(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return NotFound($"Không tìm thấy người dùng với ID {userId}.");
            }

            var userSessions = _context.ChargingSessions.Where(cs => cs.UserId == userId && cs.EndTime != null);

            if (!await userSessions.AnyAsync())
            {
                return Ok(new UserChargingStatsDto
                {
                    UserId = userId,
                    TotalSessions = 0,
                    TotalEnergyConsumedKWh = 0,
                    TotalCost = 0
                });
            }

            var stats = await userSessions
                .GroupBy(cs => cs.UserId)
                .Select(g => new UserChargingStatsDto
                {
                    UserId = g.Key,
                    TotalSessions = g.Count(),
                    TotalEnergyConsumedKWh = g.Sum(cs => cs.EnergyConsumedKWh),
                    TotalCost = g.Sum(cs => cs.TotalCost)
                })
                .FirstOrDefaultAsync();

            return Ok(stats);
        }

        // POST: api/ChargingSessions/start
        /// <summary>
        /// Bắt đầu một phiên sạc mới (ví dụ: kích hoạt từ ứng dụng).
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<ChargingSessionDto>> StartChargingSession(StartSessionDto startDto)
        {
            var user = await _context.Users.FindAsync(startDto.UserId);
            if (user == null)
            {
                return NotFound($"Không tìm thấy người dùng với ID {startDto.UserId}.");
            }

            var connector = await _context.Connectors.FindAsync(startDto.ConnectorId);
            if (connector == null)
            {
                return NotFound($"Không tìm thấy cổng sạc với ID {startDto.ConnectorId}.");
            }

            if (connector.Status != ChargingStationStatus.Available)
            {
                return BadRequest($"Cổng sạc hiện không sẵn sàng. Trạng thái: {connector.Status}.");
            }

            var newSession = new ChargingSession
            {
                UserId = startDto.UserId,
                ConnectorId = startDto.ConnectorId,
                StartTime = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = "AppWallet",
                AuthorizationIdTag = user.Code // Giả định dùng User Code để xác thực
            };

            try
            {
                connector.Status = ChargingStationStatus.Charging;
                _context.ChargingSessions.Add(newSession);
                await _context.SaveChangesAsync();

                var createdSession = await _context.ChargingSessions
                    .Include(cs => cs.User)
                    .Include(cs => cs.Connector)
                        .ThenInclude(c => c.ChargingStation)
                    .FirstAsync(cs => cs.Id == newSession.Id);

                return CreatedAtAction(nameof(GetChargingSession), new { id = newSession.Id }, new ChargingSessionDto(createdSession));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"!!! LỖI DATABASE: Không thể tạo ChargingSession cho UserId '{startDto.UserId}' tại ConnectorId '{startDto.ConnectorId}'. Lỗi: {ex.Message}");
                Console.ResetColor();
                return StatusCode(500, "Lỗi server nội bộ khi cố gắng tạo phiên sạc.");
            }
        }



        // PUT: api/ChargingSessions/5/end
        /// <summary>
        /// Kết thúc một phiên sạc và thực hiện thanh toán (ví dụ: kích hoạt từ ứng dụng).
        /// </summary>
        /// <param name="id">ID của phiên sạc đang diễn ra.</param>
        /// <param name="endDto">Thông tin khi kết thúc phiên sạc.</param>
        [HttpPut("{id}/end")]
        public async Task<IActionResult> EndChargingSession(int id, EndSessionDto endDto)
        {
            var session = await _context.ChargingSessions
                .Include(cs => cs.User)
                .Include(cs => cs.Connector)
                    .ThenInclude(c => c.ChargingStation)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (session == null)
            {
                return NotFound($"Không tìm thấy phiên sạc với ID {id}.");
            }

            if (session.EndTime.HasValue)
            {
                return BadRequest("Phiên sạc này đã kết thúc từ trước.");
            }

            var station = session.Connector.ChargingStation;
            var user = session.User;
            var totalCost = endDto.EnergyConsumedKWh * station.DefaultPricePerKWh;

            session.EndTime = DateTime.UtcNow;
            session.EnergyConsumedKWh = endDto.EnergyConsumedKWh;
            session.TotalCost = totalCost;

            if (user.Balance >= totalCost)
            {
                user.Balance -= totalCost;
                session.PaymentStatus = PaymentStatus.Paid;
                // SỬA LỖI: Sử dụng tên thuộc tính mới
                session.PaymentGatewayTransactionId = $"WALLET_TXN_{Guid.NewGuid()}";
            }
            else
            {
                session.PaymentStatus = PaymentStatus.Failed;
            }

            session.Connector.Status = ChargingStationStatus.Available;

            await _context.SaveChangesAsync();

            return Ok(new ChargingSessionDto(session));
        }
    }
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

}
