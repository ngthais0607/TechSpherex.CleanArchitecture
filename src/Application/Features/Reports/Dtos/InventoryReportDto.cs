using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record InventoryReportDto(PagedResult<InventoryGroupDto> Groups, int TotalContainers);
