using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;
