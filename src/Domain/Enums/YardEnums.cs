using System.Text.Json.Serialization;

namespace SnpDepotManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum YardBlockType
{
    Standard,
    Virtual
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VehicleType
{
    Truck,
    Barge,
    Train,
    Other
}
