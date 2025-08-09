using Backend.Data;
using Backend.DTOs;
using Backend.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/admin")]
    [ApiController]
    // [Authorize(Roles = "Admin")] // Bật lại dòng này khi có hệ thống phân quyền đầy đủ
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả các trạm sạc để admin quản lý, có thể lọc theo trạng thái.
        /// </summary>
        /// <param name="status">Lọc theo trạng thái: Pending, Approved, Rejected. Để trống để lấy tất cả.</param>
        [HttpGet("stations")]
        public async Task<IActionResult> GetStationsForAdmin([FromQuery] string? status)
        {
            var query = _context.ChargingStations
                                .Include(s => s.Owner) // Lấy thông tin người sở hữu
                                .Include(s => s.Connectors) // Lấy thông tin cổng sạc
                                .AsQueryable();

            // Xử lý lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var statusEnum))
            {
                query = query.Where(s => s.ApprovalStatus == statusEnum);
            }

            var stations = await query
                .Select(s => new AdminStationViewDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    OperatingHours = s.OperatingHours,
                    ApprovalStatus = s.ApprovalStatus.ToString(),
                    CreatedAt = s.CreatedAt,
                    OwnerId = s.Owner.Id,
                    OwnerFullName = s.Owner.FullName,
                    OwnerEmail = s.Owner.Email,
                    Connectors = s.Connectors.Select(c => new ConnectorInfo
                    {
                        ConnectorType = c.ConnectorType,
                        MaxPowerKW = c.MaxPowerKW,
                        // Giả sử giá được lấy từ trạm
                        Price = s.DefaultPricePerKWh
                    }).ToList()
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(stations);
        }

        /// <summary>
        /// Cập nhật trạng thái phê duyệt (Approved/Rejected) cho một trạm sạc.
        /// </summary>
        /// <param name="id">ID của trạm sạc</param>
        /// <param name="dto">Thông tin trạng thái mới</param>
        [HttpPut("stations/{id}/status")]
        public async Task<IActionResult> UpdateStationApproval(int id, [FromBody] UpdateApprovalStatusDto dto)
        {
            var station = await _context.ChargingStations.FindAsync(id);

            if (station == null)
            {
                return NotFound("Không tìm thấy trạm sạc.");
            }

            if (Enum.TryParse<ApprovalStatus>(dto.NewStatus, true, out var newStatusEnum))
            {
                station.ApprovalStatus = newStatusEnum;

                // Nếu phê duyệt, đánh dấu trạm là có thể hoạt động
                if (newStatusEnum == ApprovalStatus.Approved)
                {
                    station.IsOperational = true;
                }
                else
                {
                    station.IsOperational = false;
                }

                station.UpdatedAt = DateTime.UtcNow;
                // TODO: Lưu lại lý do từ chối (dto.RejectionReason) nếu cần

                await _context.SaveChangesAsync();
                return NoContent(); // Trả về 204 No Content khi thành công
            }

            return BadRequest("Trạng thái mới không hợp lệ. Vui lòng sử dụng 'Approved' hoặc 'Rejected'.");
        }
    }
}