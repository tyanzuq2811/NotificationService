using ConsumerService;
using ConsumerService.Services;
using ConsumerService.Strategies;
using MongoDB.Driver;

Common.EnvLoader.Load();

var builder = Host.CreateApplicationBuilder(args);

// Đăng ký bộ nhớ đệm MemoryCache phục vụ vô hiệu hóa cache phân tán
builder.Services.AddMemoryCache();

// Đăng ký MongoDB Client cho Read Model
var mongoConnStr = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnStr));

// Đăng ký các chiến lược phát tán thông báo (Strategy Pattern - 6 kênh độc lập)
builder.Services.AddSingleton<INotificationChannelStrategy, InAppChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, EmailChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, TelegramChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, ZaloChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, WebPushChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, WebhookChannelStrategy>();

// Đăng ký Factory khởi tạo chiến lược theo kênh
builder.Services.AddSingleton<ChannelStrategyFactory>();

// Đăng ký dịch vụ điều phối và chống chịu lỗi (Dispatcher)
builder.Services.AddSingleton<NotificationDispatcher>();

// Đăng ký tiến trình chạy ngầm lắng nghe Kafka (Background Worker)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
