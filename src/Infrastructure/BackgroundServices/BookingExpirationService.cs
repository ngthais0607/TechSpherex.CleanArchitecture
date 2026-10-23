using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.BackgroundServices;

public sealed class BookingExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public BookingExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingExpirationService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireOutdatedBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring outdated delivery order bookings.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("BookingExpirationService is stopping.");
    }

    private async Task ExpireOutdatedBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var expiredBookings = await context.Bookings
            .Join(
                context.DeliveryOrders,
                b => b.OrderId,
                o => o.Id,
                (b, o) => new { Booking = b, o.ExpiryDate })
            .Where(x => x.Booking.Status == BookingStatus.Reserved && x.ExpiryDate < utcNow)
            .Select(x => x.Booking)
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count > 0)
        {
            foreach (var booking in expiredBookings)
            {
                booking.Expire(utcNow);
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Expired {Count} outdated container bookings.", expiredBookings.Count);
        }
    }
}
