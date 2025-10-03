using Backend.Model;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Services
{
    public class JwtService
    {
        private readonly string _secretKey; // Chìa khóa bí mật, phải đủ dài và phức tạp
        private readonly string _issuer;    // Tên đơn vị phát hành token
        private readonly string _audience;  // Tên đối tượng được phép sử dụng token

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["Jwt:Key"];
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];
        }
        public string GenerateJwtToken(User user)
        {
            // 1. Tạo Token Handler: Đối tượng chính để làm việc với JWT
            var tokenHandler = new JwtSecurityTokenHandler();

            // 2. Chuẩn bị khóa bí mật
            var key = Encoding.ASCII.GetBytes(_secretKey);

            // 3. Tạo danh sách các "Claims" - thông tin chứa trong token
            // Đây là phần quan trọng nhất, quyết định token chứa dữ liệu gì.
            var claims = new List<Claim>
        {
            // --- Standard Claims (Các claim tiêu chuẩn) ---
            new Claim(JwtRegisteredClaimNames.Sub, user.Code), // Subject: ID của người dùng, là định danh duy nhất
            new Claim(JwtRegisteredClaimNames.Email, user.Email), // Email
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID: Một ID duy nhất cho token này để tránh tấn công phát lại (replay attack)
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(), ClaimValueTypes.Integer64), // Issued At: Thời điểm token được phát hành

            // --- Custom Claims (Các claim tùy chỉnh) ---
            new Claim("fullName", user.FullName) // Ví dụ: lưu tên đầy đủ
        };

            // Thêm các role của người dùng vào claims
            claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));


            // 4. Tạo Token Descriptor: Mô tả tất cả thông tin về token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(100), // Thời gian hết hạn của token (ví dụ: 1 giờ)
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            // 5. Tạo Token
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 6. Chuyển token thành dạng chuỗi để gửi về cho client
            return tokenHandler.WriteToken(token);
        }
    }
}
