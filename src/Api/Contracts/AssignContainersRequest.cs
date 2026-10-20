namespace SnpDepotManagement.Api.Contracts;

public sealed record AssignContainersRequest(IReadOnlyList<string> ContainerNumbers);
