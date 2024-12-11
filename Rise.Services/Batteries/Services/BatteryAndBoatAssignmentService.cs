using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rise.Shared.Emails;

namespace Rise.Services.Batteries
{
    /// <summary>
    /// Achtergrondservice die periodiek het batterijtoewijzingsproces start.
    /// </summary>
    public class BatteryAndBoatAssignmentService : BackgroundService
    {
        private readonly ILogger<BatteryAndBoatAssignmentService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IEmailService _emailService;

        /// <summary>
        /// Initialiseert een nieuwe instantie van de BatteryAssignmentService klasse.
        /// </summary>
        /// <param name="logger">De logger om informatie en fouten vast te leggen.</param>
        /// <param name="scopeFactory">De service scope factory voor het maken van nieuwe scopes.</param>
        public BatteryAndBoatAssignmentService(
            ILogger<BatteryAndBoatAssignmentService> logger,
            IServiceScopeFactory scopeFactory
        )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Start de periodieke uitvoering van het batterijtoewijzingsproces.
        /// </summary>
        /// <param name="stoppingToken">Token om de achtergrondtaak te annuleren.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Battery Assignment Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Battery Assignment Service is running.");

                    // Create a new scope for each execution
                    using var scope = _scopeFactory.CreateScope();
                    var processor =
                        scope.ServiceProvider.GetRequiredService<IBatteryAndBoatAssignmentProcessor>();
                    await processor.ProcessBatteryAndBoatAssignmentsAsync();

                    var now = DateTime.Now;
                    var nextRun = now.Date.AddHours(0);

                    if (now >= nextRun)
                    {
                        nextRun = nextRun.AddDays(1);
                    }

                    var delay = nextRun - now;

                    // Wait until the next day 8 am --- CHANGE TO 5000 FOR TESTING
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in battery assignment service");
                }
            }
        }
    }
}
