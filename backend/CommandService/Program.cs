using CommandService.Services;
using Microsoft.OpenApi.Models;

Common.EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Thêm Controllers và cấu hình tài liệu Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notification Command API (.NET 8 CQRS)",
        Version = "v1",
        Description = "Microservice xử lý tiếp nhận thông báo và Transactional Outbox Pattern"
    });
});

// Đăng ký bộ nhớ đệm MemoryCache
builder.Services.AddMemoryCache();

// Thiết lập chính sách CORS cho Frontend Developer Console
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Đăng ký các dịch vụ tầng Domain và Hạ tầng (Infrastructure)
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<INotificationCommandService, NotificationCommandService>();

// Đăng ký các tiến trình chạy ngầm (Outbox Relay Worker và Dọn dẹp dữ liệu)
builder.Services.AddHostedService<OutboxRelayWorker>();
builder.Services.AddHostedService<OutboxCleanupWorker>();

var app = builder.Build();

// Tự động khởi tạo cấu trúc CSDL và nạp dữ liệu mẫu ban đầu
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Starting DatabaseInitializer...");
        await initializer.InitializeAsync();
        logger.LogInformation("DatabaseInitializer finished successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DatabaseInitializer failed on startup.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Command API v1");
    c.RoutePrefix = string.Empty; // Mở Swagger trực tiếp tại URL gốc
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
