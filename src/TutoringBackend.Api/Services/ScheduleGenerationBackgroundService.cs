namespace TutoringBackend.Api.Services;

/// Co godzinę dogenerowuje brakujące lekcje z aktywnych cyklicznych slotów. żeby uniknąć niechcianego momętu że nie ustawi gdzieś lekcji
public class ScheduleGenerationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduleGenerationBackgroundService> _logger;

    public ScheduleGenerationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ScheduleGenerationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IScheduleGenerationService>();
                var created = await service.GenerateUpcomingLessonsAsync(ct: stoppingToken);
                if (created > 0)
                {
                    _logger.LogInformation("Wygenerowano {Count} nowych lekcji z harmonogramu.", created);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas generowania lekcji z harmonogramu.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
