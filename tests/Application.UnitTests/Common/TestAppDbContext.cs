using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.UnitTests.Common;

public class TestAppDbContext : DbContext, IAppDbContext
{
    public TestAppDbContext(DbContextOptions<TestAppDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ContainerMovement>(builder =>
        {
            builder.OwnsOne(m => m.FromPosition);
            builder.OwnsOne(m => m.ToPosition);
        });

        modelBuilder.Entity<Container>(builder =>
        {
            builder.Property(c => c.ContainerNumber)
                .HasConversion(
                    cn => cn.Value,
                    val => SnpDepotManagement.Domain.ValueObjects.ContainerNumber.Create(val).Value!);
        });

        modelBuilder.Entity<YardSlot>(builder =>
        {
            builder.OwnsOne(s => s.Position);
        });

        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.HasMany(u => u.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId);

            builder.Navigation(u => u.UserRoles)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<YardBlock>(builder =>
        {
            builder.HasMany(b => b.Slots)
                .WithOne()
                .HasForeignKey(s => s.BlockId);

            builder.Navigation(b => b.Slots)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DeliveryOrder>(builder =>
        {
            builder.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.DeliveryOrderId);

            builder.Navigation(o => o.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is ITenantEntity tenant && string.IsNullOrWhiteSpace(tenant.TenantId))
                tenant.TenantId = "TEST-DEPOT";
            if (entry.State == EntityState.Modified && entry.GetDatabaseValues() == null)
            {
                entry.State = EntityState.Added;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public static TestAppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestAppDbContext(options);
    }
}
