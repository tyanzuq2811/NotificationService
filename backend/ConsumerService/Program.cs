using ConsumerService;
using ConsumerService.Services;
using ConsumerService.Strategies;
using MongoDB.Driver;

Common.EnvLoader.Load();

var builder = Host.CreateApplicationBuilder(args);

// Register MemoryCache for Distributed Cache Invalidation
builder.Services.AddMemoryCache();

// Register MongoDB Client
var mongoConnStr = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnStr));

// Register Channel Strategies (Strategy Pattern - 6 Channels)
builder.Services.AddSingleton<INotificationChannelStrategy, InAppChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, EmailChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, TelegramChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, ZaloChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, WebPushChannelStrategy>();
builder.Services.AddSingleton<INotificationChannelStrategy, WebhookChannelStrategy>();

// Register Factory
builder.Services.AddSingleton<ChannelStrategyFactory>();

// Register Dispatcher Service
builder.Services.AddSingleton<NotificationDispatcher>();

// Register Background Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
