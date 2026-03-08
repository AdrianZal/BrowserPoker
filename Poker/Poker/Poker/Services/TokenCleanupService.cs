using Poker.Models;
using Microsoft.EntityFrameworkCore;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromHours(13);

    public TokenCleanupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PokerContext>();

                var now = DateTime.UtcNow;

                int deletedCount = await dbContext.RefreshTokens
                    .Where(t => t.ExpiresAt < now || t.Revoked)
                    .ExecuteDeleteAsync(stoppingToken);

                Console.WriteLine($"[{DateTime.Now}] Cleared {deletedCount} tokens.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Toker cleanup error: {ex.Message}");
            }
        }
    }
}