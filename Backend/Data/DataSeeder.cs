// Data/DataSeeder.cs
using Backend.Model;

namespace Backend.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Đảm bảo database đã được tạo
            context.Database.EnsureCreated();

            // Kiểm tra xem đã có tài khoản Admin nào chưa
            if (!context.Users.Any(u => u.Role == UserRole.Admin))
            {
                // Nếu chưa có, tạo một tài khoản Admin mặc định
                var adminUser = new User
                {
                    // Lưu ý: Trong thực tế, bạn sẽ không set Id trực tiếp nếu nó là cột tự tăng.
                    // EF Core sẽ xử lý việc này. User đầu tiên được thêm sẽ có Id = 1.
                    Username = "admin",
                    // Mật khẩu cần được băm (hashed) trong một ứng dụng thực tế.
                    // Ở đây chúng ta dùng một chuỗi đơn giản để minh họa.
                    PasswordHash = "hashed_admin_password",
                    FullName = "System Administrator",
                    Email = "admin@example.com",
                    PhoneNumber = "0123456789",
                    Role = UserRole.Admin,
                    Balance = 0
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }
        }
    }
}