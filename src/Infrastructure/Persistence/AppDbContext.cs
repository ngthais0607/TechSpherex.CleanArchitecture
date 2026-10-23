using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.ValueObjects;
using SnpDepotManagement.Infrastructure.Persistence.Seed;

namespace SnpDepotManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private static readonly MethodInfo SetQueryFilterMethod = typeof(AppDbContext)
        .GetMethod(nameof(SetQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ITenantProvider _tenantProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        ICurrentUser currentUser) : base(options)
    {
        _tenantProvider = tenantProvider;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public DbSet<Container> Containers => Set<Container>();
    public DbSet<YardBlock> YardBlocks => Set<YardBlock>();
    public DbSet<YardSlot> YardSlots => Set<YardSlot>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderItem> DeliveryOrderItems => Set<DeliveryOrderItem>();
    public DbSet<ContainerLifecycle> ContainerLifecycles => Set<ContainerLifecycle>();
    public DbSet<ContainerMovement> ContainerMovements => Set<ContainerMovement>();
    public DbSet<LineOperator> LineOperators => Set<LineOperator>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Voyage> Voyages => Set<Voyage>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<LineOperatorOwnerCode> LineOperatorOwnerCodes => Set<LineOperatorOwnerCode>();
    public DbSet<DeliveryOrderBooking> Bookings => Set<DeliveryOrderBooking>();
    public DbSet<GateTransaction> GateTransactions => Set<GateTransaction>();
    public DbSet<GateTransactionChange> GateTransactionChanges => Set<GateTransactionChange>();
    public DbSet<DemoSeedRun> DemoSeedRuns => Set<DemoSeedRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply global query filter for all ITenantEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetQueryFilterMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetQueryFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantProvider.GetCurrentTenantId());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        var currentUserId = _currentUser.UserId?.ToString();
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        // Occupancy/support and quota/completion are aggregate invariants. Every child write
        // also updates the parent read by the handler, so its xmin protects disjoint children.
        // The parent update shares the same SaveChanges transaction as slots/items/history.
        var changedBlocks = ChangeTracker.Entries<YardSlot>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.BlockId).ToHashSet();
        foreach (var block in ChangeTracker.Entries<YardBlock>().Where(e => changedBlocks.Contains(e.Entity.Id) && e.State != EntityState.Added))
        {
            block.Property(b => b.LastModifiedAt).CurrentValue = utcNow;
            block.Property(b => b.LastModifiedAt).IsModified = true;
        }
        var changedOrders = ChangeTracker.Entries<DeliveryOrderItem>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.DeliveryOrderId).ToHashSet();
        foreach (var order in ChangeTracker.Entries<DeliveryOrder>().Where(e => changedOrders.Contains(e.Entity.Id) && e.State != EntityState.Added))
        {
            order.Property(o => o.LastModifiedAt).CurrentValue = utcNow;
            order.Property(o => o.LastModifiedAt).IsModified = true;
        }

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is GateTransactionChange && entry.State == EntityState.Modified)
                entry.State = EntityState.Added;
            if ((entry.Entity is ContainerMovement or MovementPosition) && entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Container movement history is append-only.");
            if (entry.Entity is ITenantEntity tenantEntity && entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(tenantEntity.TenantId) && !string.IsNullOrWhiteSpace(currentTenantId))
                {
                    tenantEntity.TenantId = currentTenantId;
                }
            }

            if (entry.Entity is AuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedAt = utcNow;
                    if (!string.IsNullOrWhiteSpace(currentUserId) && string.IsNullOrWhiteSpace(auditableEntity.CreatedBy))
                    {
                        auditableEntity.CreatedBy = currentUserId;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.LastModifiedAt = utcNow;
                    if (!string.IsNullOrWhiteSpace(currentUserId))
                    {
                        auditableEntity.LastModifiedBy = currentUserId;
                    }
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
