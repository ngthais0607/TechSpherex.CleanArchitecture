using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.Abstractions.Data;

public interface IAppDbContext
{
    DbSet<Container> Containers { get; }
    DbSet<YardBlock> YardBlocks { get; }
    DbSet<YardSlot> YardSlots { get; }
    DbSet<DeliveryOrder> DeliveryOrders { get; }
    DbSet<DeliveryOrderItem> DeliveryOrderItems { get; }
    DbSet<ContainerLifecycle> ContainerLifecycles { get; }
    DbSet<LineOperator> LineOperators { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Voyage> Voyages { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
