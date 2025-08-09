using Backend.Model;
using Microsoft.EntityFrameworkCore;
namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<ChargingStation> ChargingStations { get; set; }
        public DbSet<Connector> Connectors { get; set; }
        public DbSet<ChargingSession> ChargingSessions { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; }

        // Ghi đè phương thức này để cấu hình model bằng Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Cấu hình cho thực thể User ---
            modelBuilder.Entity<User>(entity =>
            {
                // Thiết lập Username và Email là duy nhất (unique)
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();

                // Một User (Agent) có thể sở hữu nhiều ChargingStation
                entity.HasMany(u => u.OwnedStations)
                      .WithOne(cs => cs.Owner)
                      .HasForeignKey(cs => cs.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict); // Ngăn việc xóa User nếu họ vẫn còn sở hữu trạm sạc
            });

            // --- Cấu hình cho thực thể ChargingStation ---
            modelBuilder.Entity<ChargingStation>(entity =>
            {
                // Một ChargingStation có nhiều Connector
                entity.HasMany(cs => cs.Connectors)
                      .WithOne(c => c.ChargingStation)
                      .HasForeignKey(c => c.ChargingStationId)
                      .OnDelete(DeleteBehavior.Cascade); // Xóa các Connector nếu trạm sạc bị xóa
            });

            // --- Cấu hình cho thực thể ChargingSession ---
            modelBuilder.Entity<ChargingSession>(entity =>
            {
                // Cấu hình độ chính xác cho cột TotalCost
                entity.Property(cs => cs.TotalCost).HasColumnType("decimal(18, 2)");
                entity.Property(cs => cs.EnergyConsumedKWh).HasColumnType("decimal(18, 2)");
            });

            // --- Cấu hình cho thực thể Review ---
            modelBuilder.Entity<Review>(entity =>
            {
                // Một người dùng không thể review cùng một trạm sạc nhiều lần
                // (Tùy theo logic nghiệp vụ, ở đây ta giả sử là được)
                // entity.HasIndex(r => new { r.UserId, r.ChargingStationId }).IsUnique();
            });

            // Bạn có thể thêm các cấu hình khác cho các thực thể còn lại ở đây
        }
    }
}
