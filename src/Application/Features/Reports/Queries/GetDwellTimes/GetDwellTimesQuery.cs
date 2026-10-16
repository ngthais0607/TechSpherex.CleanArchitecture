using FluentValidation;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetDwellTimes;

public sealed record GetDwellTimesQuery(string? LineOperator = null, int? MinDays = null,
    bool IncludeGatedOut = false, int PageIndex = 1, int PageSize = 20) : IQuery<PagedResult<DwellTimeDto>>;

public sealed class GetDwellTimesQueryValidator : AbstractValidator<GetDwellTimesQuery>
{ public GetDwellTimesQueryValidator() => RuleFor(q => q.MinDays).GreaterThanOrEqualTo(0); }
