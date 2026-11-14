using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using QueryService.Hubs;
using QueryService.Repositories;
using QueryService.Services;

Common.EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Add Controllers & Swagger
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

// Add SignalR
builder.Services.AddSignalR();

// Add CORS with Credentials for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // allows any origin in development
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register MongoDB Client
var mongoConnStr = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnStr));

// Register Repository
builder.Services.AddScoped<INotificationQueryRepository, NotificationQueryRepository>();

// Register Kafka Consumer for In-App Push events
builder.Services.AddHostedService<InAppPushRelayWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Query API v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

app.UseCors("CorsPolicy");
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
