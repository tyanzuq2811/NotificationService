using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using QueryService.Hubs;
using QueryService.Repositories;
using QueryService.Services;

Common.EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Thêm Controllers và cấu hình tài liệu Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notification Query API & SignalR Hub (.NET 8 CQRS)",
        Version = "v1",
        Description = "Microservice truy vấn dữ liệu từ MongoDB và đẩy thông báo Realtime In-App qua SignalR"
    });
});

// Đăng ký dịch vụ SignalR Hub thời gian thực
builder.Services.AddSignalR();

// Cấu hình chính sách CORS kèm xác thực thông tin cho SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Cho phép mọi nguồn gốc trong môi trường phát triển
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Đăng ký MongoDB Client cho Read Model
var mongoConnStr = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnStr));

// Đăng ký Repository truy vấn dữ liệu
builder.Services.AddScoped<INotificationQueryRepository, NotificationQueryRepository>();

// Đăng ký tiến trình chạy ngầm lắng nghe sự kiện In-App Push từ Kafka
builder.Services.AddHostedService<InAppPushRelayWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Query API v1");
    c.RoutePrefix = string.Empty; // Mở Swagger trực tiếp tại URL gốc
});

app.UseCors("CorsPolicy");
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
