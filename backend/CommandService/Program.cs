using CommandService.Services;
using Microsoft.OpenApi.Models;

Common.EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Add Controllers & Swagger
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

// Add MemoryCache
builder.Services.AddMemoryCache();

// Add CORS for Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register Domain & Infrastructure Services
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<INotificationCommandService, NotificationCommandService>();

// Register Background Workers for Outbox Pattern & Data Retention
builder.Services.AddHostedService<OutboxRelayWorker>();
builder.Services.AddHostedService<OutboxCleanupWorker>();

var app = builder.Build();

// Auto-initialize Database & Seed Data
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
    c.RoutePrefix = string.Empty; // Swagger at root URL
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
