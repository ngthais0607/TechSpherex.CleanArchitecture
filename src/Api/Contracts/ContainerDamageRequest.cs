using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Api.Contracts;

public sealed record ContainerDamageRequest(ContainerCondition Condition = ContainerCondition.Damaged);
