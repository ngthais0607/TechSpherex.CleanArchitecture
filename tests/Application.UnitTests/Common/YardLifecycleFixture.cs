using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Application.UnitTests.Common;

internal static class YardLifecycleFixture
{
    // Existing yard fixtures predate the requirement that placement has an intake lifecycle.
    internal static void TrackActiveLifecycles(TestAppDbContext context)
    {
        foreach (var container in context.Containers.Local.ToList())
        {
            if (context.ContainerLifecycles.Local.Any(l => l.ContainerId == container.Id)) continue;
            var slot = context.YardSlots.Local.Where(s => s.ContainerId == container.Id).OrderBy(s => s.Bay).FirstOrDefault();
            var lifecycle = ContainerLifecycle.GateIn(container.Id, container.OwnerCode, container.Classification,
                container.Condition, "TEST-TRUCK", slot?.Id ?? Guid.NewGuid(), DateTime.UtcNow,
                string.IsNullOrWhiteSpace(container.TenantId) ? "TEST-DEPOT" : container.TenantId);
            lifecycle.SetCurrentSlot(slot?.Id);
            context.ContainerLifecycles.Add(lifecycle);
        }
    }
}
