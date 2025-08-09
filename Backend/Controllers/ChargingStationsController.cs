using Backend.Data;
using Backend.DTOs;
using Backend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [Route("api/charging-stations")]
    [ApiController]
    public class ChargingStationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChargingStationsController(AppDbContext context)
        {
            _context = context;
        }

        // === CRUD cho ChargingStation ===

        // GET: api/charging-stations
        [HttpGet]
        public async Task<IActionResult> GetStations()
        {
            var stations = await _context.ChargingStations
                .Include(s => s.Connectors)
                .Select(s => new ChargingStationDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    OperatingHours = s.OperatingHours,
                    OwnerId = s.OwnerId,
                    ApprovalStatus = s.ApprovalStatus.ToString(),
                    Connectors = s.Connectors.Select(c => new ConnectorDto
                    {
                        Id = c.Id,
                        ConnectorType = c.ConnectorType,
                        MaxPowerKW = c.MaxPowerKW,
                        Status = c.Status.ToString()
                    }).ToList()
                })
                .ToListAsync();

            return Ok(stations);
        }

        // GET: api/charging-stations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStationById(int id)
        {
            var station = await _context.ChargingStations
                .Where(s => s.Id == id)
                .Include(s => s.Connectors)
                .Select(s => new ChargingStationDto // <-- ĐÃ HOÀN THIỆN PHẦN MAPPING
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    OperatingHours = s.OperatingHours,
                    OwnerId = s.OwnerId,
                    ApprovalStatus = s.ApprovalStatus.ToString(),
                    Connectors = s.Connectors.Select(c => new ConnectorDto
                    {
                        Id = c.Id,
                        ConnectorType = c.ConnectorType,
                        MaxPowerKW = c.MaxPowerKW,
                        Status = c.Status.ToString()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (station == null)
            {
                return NotFound();
            }

            return Ok(station);
        }

        // GET: api/charging-stations/by-user/1
        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetStationsByUserId(int userId)
        {
            var stations = await _context.ChargingStations
                .Where(s => s.OwnerId == userId)
                .Include(s => s.Connectors)
                .Select(s => new StationSummaryDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    ApprovalStatus = s.ApprovalStatus.ToString(),
                    TotalConnectors = s.Connectors.Count(),
                    AvailableConnectors = s.Connectors.Count(c => c.Status == ChargingStationStatus.Available)
                })
                .ToListAsync();

            return Ok(stations);
        }

        // POST: api/charging-stations
        [HttpPost]
        // [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> CreateStation([FromForm] CreateStationFormDto stationDto)
        {
            // === Bước 1: Xác thực và lấy thông tin chủ sở hữu ===
            // Tạm thời gán cứng OwnerId để test.
            // Khi có hệ thống đăng nhập, bạn sẽ lấy từ token.
            // var ownerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (string.IsNullOrEmpty(ownerIdString))
            // {
            //     return Unauthorized("Không tìm thấy thông tin người dùng.");
            // }
            // var ownerId = int.Parse(ownerIdString);
            var ownerIdForTesting = 2;


            // === Bước 2: Xử lý dữ liệu đầu vào ===
            List<ConnectorInfo> connectorInfos;
            try
            {
            {
                // Deserialize chuỗi JSON của connectors
                connectorInfos = JsonSerializer.Deserialize<List<ConnectorInfo>>(
                    stationDto.Connectors,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (JsonException)
            {
                return BadRequest("Định dạng dữ liệu cổng sạc (connectors) không hợp lệ.");
            }

            if (connectorInfos == null || !connectorInfos.Any())
            {
                return BadRequest("Cần cung cấp thông tin cho ít nhất một cổng sạc.");
            }

            // TODO: Xử lý lưu trữ file (stationDto.StationImages, stationDto.BusinessLicense)
            // TODO: Geocoding địa chỉ để lấy kinh độ, vĩ độ nếu cần

            // === Bước 3: Tạo và lưu các đối tượng vào Database ===
            var station = new ChargingStation
            {
                Name = stationDto.Name,
                Address = stationDto.Address,
                OperatingHours = stationDto.OperatingHours,
                OwnerId = ownerIdForTesting,
                ApprovalStatus = ApprovalStatus.Pending, // Luôn là "Chờ duyệt" khi mới tạo
                IsOperational = false, // Chưa hoạt động cho đến khi được duyệt
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DefaultPricePerKWh = connectorInfos.First().Price, // Lấy giá của cổng đầu tiên làm mặc định
                Connectors = connectorInfos.Select(info => new Connector
                {
                    ConnectorType = info.ConnectorType,
                    MaxPowerKW = info.MaxPowerKW,
                    Status = ChargingStationStatus.Unavailable // Mặc định là không sẵn sàng
                }).ToList()
            };

            await _context.ChargingStations.AddAsync(station);
            await _context.SaveChangesAsync();


            // === Bước 4: Tạo và trả về Response DTO (quan trọng nhất) ===
            // Ánh xạ (map) entity `station` sang `StationResponseDto` để tránh lỗi vòng lặp
            var responseDto = new StationResponseDto
            {
                Id = station.Id,
                Name = station.Name,
                Address = station.Address,
                OperatingHours = station.OperatingHours,
                ApprovalStatus = station.ApprovalStatus.ToString(),
                OwnerId = station.OwnerId,
                Connectors = station.Connectors.Select(c => new ConnectorResponseDto
                {
                    Id = c.Id,
                    ConnectorType = c.ConnectorType,
                    MaxPowerKW = c.MaxPowerKW,
                    Status = c.Status.ToString()
                }).ToList()
            };

            // Trả về status 201 Created cùng với thông tin trạm vừa tạo (dưới dạng DTO)
            return CreatedAtAction(nameof(GetStationById), new { id = station.Id }, responseDto);
        }

        // PUT: api/charging-stations/5
        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> UpdateStation(int id, [FromBody] CreateUpdateChargingStationDto stationDto)
        {
            var station = await _context.ChargingStations.FindAsync(id);
            if (station == null)
            {
                return NotFound();
            }

            // var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            // if (station.OwnerId != userId && !User.IsInRole("Admin"))
            // {
            //     return Forbid();
            // }

            // --- ĐÃ HOÀN THIỆN PHẦN CẬP NHẬT ---
            station.Name = stationDto.Name;
            station.Address = stationDto.Address;
            station.Latitude = stationDto.Latitude;
            station.Longitude = stationDto.Longitude;
            station.OperatingHours = stationDto.OperatingHours;
            // Lưu ý: Không cho phép cập nhật OwnerId hoặc ApprovalStatus ở đây.
            // Việc duyệt trạm nên có một endpoint riêng cho Admin.

            await _context.SaveChangesAsync();
            return NoContent(); // Trả về 204 No Content khi cập nhật thành công
        }

        // DELETE: api/charging-stations/5
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> DeleteStation(int id)
        {
            var station = await _context.ChargingStations.FindAsync(id);
            if (station == null)
            {
                return NotFound();
            }

            _context.ChargingStations.Remove(station);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // === CRUD cho Connector (lồng trong Station) ===

        // GET: api/charging-stations/5/connectors
        [HttpGet("{stationId}/connectors")]
        public async Task<IActionResult> GetConnectorsForStation(int stationId)
        {
            if (!await _context.ChargingStations.AnyAsync(s => s.Id == stationId))
            {
                return NotFound("Charging station not found.");
            }

            var connectors = await _context.Connectors
                .Where(c => c.ChargingStationId == stationId)
                .Select(c => new ConnectorDto
                {
                    Id = c.Id,
                    ConnectorType = c.ConnectorType,
                    MaxPowerKW = c.MaxPowerKW,
                    Status = c.Status.ToString()
                })
                .ToListAsync();

            return Ok(connectors);
        }


        // POST: api/charging-stations/5/connectors
        [HttpPost("{stationId}/connectors")]
        // [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> AddConnector(int stationId, [FromBody] CreateUpdateConnectorDto connectorDto)
        {
            var station = await _context.ChargingStations.FindAsync(stationId);
            if (station == null)
            {
                return NotFound("Charging station not found.");
            }

            var connector = new Connector
            {
                ChargingStationId = stationId,
                ConnectorType = connectorDto.ConnectorType,
                MaxPowerKW = connectorDto.MaxPowerKW,
                Status = connectorDto.Status
            };

            await _context.Connectors.AddAsync(connector);
            await _context.SaveChangesAsync();

            var resultDto = new ConnectorDto
            {
                Id = connector.Id,
                ConnectorType = connector.ConnectorType,
                MaxPowerKW = connector.MaxPowerKW,
                Status = connector.Status.ToString()
            };

            return Ok(resultDto);
        }

        // PUT: api/charging-stations/5/connectors/10
        [HttpPut("{stationId}/connectors/{connectorId}")]
        // [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> UpdateConnector(int stationId, int connectorId, [FromBody] CreateUpdateConnectorDto connectorDto)
        {
            var connector = await _context.Connectors
                .FirstOrDefaultAsync(c => c.Id == connectorId && c.ChargingStationId == stationId);

            if (connector == null)
            {
                return NotFound("Connector not found.");
            }

            connector.ConnectorType = connectorDto.ConnectorType;
            connector.MaxPowerKW = connectorDto.MaxPowerKW;
            connector.Status = connectorDto.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/charging-stations/5/connectors/10
        [HttpDelete("{stationId}/connectors/{connectorId}")]
        // [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> DeleteConnector(int stationId, int connectorId)
        {
            var connector = await _context.Connectors
                .FirstOrDefaultAsync(c => c.Id == connectorId && c.ChargingStationId == stationId);

            if (connector == null)
            {
                return NotFound("Connector not found.");
            }

            _context.Connectors.Remove(connector);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
