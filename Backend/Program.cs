using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =================================================================================
// 1. LẤY CÁC CẤU HÌNH
// =================================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// THÊM DỊCH VỤ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // Cho phép yêu cầu từ địa chỉ của ứng dụng React
                          policy.WithOrigins("http://localhost:5174")
                                .AllowAnyHeader() // Cho phép tất cả các header
                                .AllowAnyMethod(); // Cho phép tất cả các phương thức (GET, POST, PUT, DELETE...)
                      });
});


// =================================================================================
// 2. ĐĂNG KÝ CÁC SERVICES
// =================================================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient("GooglePlaces", client =>
{
    client.BaseAddress = new Uri("https://places.googleapis.com/");
});

builder.Services.AddScoped<IGooglePlacesService, GooglePlacesService>();

// Các service mặc định cho Web API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// =================================================================================
// 3. BUILD ỨNG DỤNG
// =================================================================================
var app = builder.Build();


// =================================================================================
// 4. CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE)
// Thứ tự của các middleware này rất quan trọng.
// =================================================================================

// Bật Swagger UI trong môi trường development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Chuyển hướng HTTP sang HTTPS
app.UseHttpsRedirection();

// *** SỬ DỤNG MIDDLEWARE CORS ***
// Dòng này phải được đặt trước UseAuthorization
app.UseCors(MyAllowSpecificOrigins);

// Bật middleware phân quyền
app.UseAuthorization();

// Ánh xạ các request đến đúng Controller
app.MapControllers();

// =================================================================================
// 5. CHẠY ỨNG DỤNG
// =================================================================================
app.Run();
