using System.ComponentModel.DataAnnotations;
namespace Backend.DTOs
{
    public class AuthDtos
    {
        public class LoginRequestDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        // DTO trả về giờ chỉ cần AccessToken
        public class LoginResponseDto
        {
            public string AccessToken { get; set; }
        }
    }
}
