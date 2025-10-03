using Backend.Data;
using Backend.DTOs;
using Backend.Model;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Backend.DTOs.AuthDtos;
namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService; // Giả sử JwtService đã được inject

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Endpoint để người dùng đăng nhập.
        /// </summary>
        /// <param name="request">Thông tin đăng nhập gồm username và password.</param>
        /// <returns>AccessToken nếu thành công.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // 1. Tìm người dùng trong database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // 2. Kiểm tra người dùng và mật khẩu
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không hợp lệ." });
            }

            // 3. Chuẩn bị thông tin để tạo Access Token (JWT)

            var accessToken = _jwtService.GenerateJwtToken(user);

            // 4. Trả về Access Token cho client
            return Ok(new LoginResponseDto
            {
                AccessToken = accessToken
            });
        }

    }
}
