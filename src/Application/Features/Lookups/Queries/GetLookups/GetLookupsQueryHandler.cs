using SnpDepotManagement.Application.Abstractions.Caching;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Lookups.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Lookups.Queries.GetLookups;

public sealed class GetLookupsQueryHandler : IQueryHandler<GetLookupsQuery, LookupsDto>
{
    private static readonly Dictionary<string, string> VnLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dry"] = "Container khô",
        ["Reefer"] = "Container lạnh",
        ["OpenTop"] = "Open Top",
        ["FlatRack"] = "Flat Rack",
        ["Bunker"] = "Bunker",
        ["Ventilated"] = "Thông gió",
        ["Specialized"] = "Chuyên dụng",
        ["Ft20"] = "20 feet",
        ["Ft40"] = "40 feet",
        ["Normal"] = "Bình thường",
        ["Damaged"] = "Hư hỏng",
        ["Dented"] = "Móp méo",
        ["Bent"] = "Biến dạng",
        ["Warped"] = "Vênh méo",
        ["A"] = "Loại A",
        ["B"] = "Loại B",
        ["C"] = "Loại C",
        ["Truck"] = "Xe tải",
        ["Train"] = "Tàu hỏa",
        ["Barge"] = "Sà lan",
        ["Other"] = "Khác"
    };

    private readonly ICacheService _cache;

    public GetLookupsQueryHandler(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<Result<LookupsDto>> Handle(GetLookupsQuery query, CancellationToken cancellationToken = default)
    {
        const string cacheKey = "lookups_all";
        var cached = await _cache.GetAsync<LookupsDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var sizes = Enum.GetNames<ContainerSize>()
            .ToDictionary(n => n, n => VnLabels.GetValueOrDefault(n, n));

        var types = Enum.GetNames<ContainerType>()
            .ToDictionary(n => n, n => VnLabels.GetValueOrDefault(n, n));

        var conditions = Enum.GetNames<ContainerCondition>()
            .ToDictionary(n => n, n => VnLabels.GetValueOrDefault(n, n));

        var classifications = Enum.GetNames<Classification>()
            .ToDictionary(n => n, n => VnLabels.GetValueOrDefault(n, n));

        var vehicles = Enum.GetNames<VehicleType>()
            .ToDictionary(n => n, n => VnLabels.GetValueOrDefault(n, n));

        var result = new LookupsDto(sizes, types, conditions, classifications, vehicles);
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1), cancellationToken);

        return Result.Success(result);
    }
}
