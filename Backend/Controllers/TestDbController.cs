using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestDbController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestDbController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return Ok(new { status = canConnect ? "✅ Kết nối thành công!" : "❌ Không thể kết nối" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "❌ Lỗi kết nối", message = ex.Message });
            }
        }
    }
}
