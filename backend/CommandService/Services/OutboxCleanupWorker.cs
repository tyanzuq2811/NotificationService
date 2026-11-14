using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommandService.Services
{
    public class OutboxCleanupWorker : BackgroundService
    {
        private readonly string _connectionString;
        private readonly ILogger<OutboxCleanupWorker> _logger;

        public OutboxCleanupWorker(IConfiguration config, ILogger<OutboxCleanupWorker> logger)
        {
            _connectionString = config.GetConnectionString("SqlServer")!;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxCleanupWorker started (Runs every 24 hours to purge records > 7 days old)...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync(stoppingToken);

                    var purgeSql = @"
                        DELETE FROM NotificationEvents 
                        WHERE IsPublished = 1 AND PublishedAt < DATEADD(DAY, -7, GETUTCDATE());";

                    var deletedCount = await connection.ExecuteAsync(purgeSql);
                    if (deletedCount > 0)
                    {
                        _logger.LogInformation("OutboxCleanupWorker purged {Count} historical outbox events (> 7 days).", deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OutboxCleanupWorker execution.");
                }

                // Wait 24 hours before next cleanup run
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
