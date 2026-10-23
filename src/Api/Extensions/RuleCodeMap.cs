namespace SnpDepotManagement.Api.Extensions;

/// <summary>Ánh xạ errorCode → ruleCode ngắn cho UI (D2, G4…).</summary>
public static class RuleCodeMap
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.Ordinal)
    {
        ["Container.NotFound"]                    = "D1",
        ["Container.AlreadyExists"]               = "D2",
        ["Container.InvalidIsoCode"]              = "D3",
        ["Container.IsoSizeMismatch"]             = "D3a",
        ["Container.FutureManufactureDate"]       = "D4",
        ["OrderRule.UnknownLineOperator"]         = "D5",
        ["OrderRule.UnknownCustomer"]             = "D6",
        ["OrderRule.ExpiryInPast"]                = "D7",
        ["OrderRule.DuplicateLine"]               = "D8",
        ["OrderRule.LineOperatorMismatch"]        = "D2",
        ["GateOut.WrongOrderReservation"]         = "G4",
        ["Booking.ContainerAlreadyReserved"]      = "D3",
        ["Booking.QuantityExceeded"]              = "D2",
        ["Booking.QuotaExceeded"]                 = "D2",
        ["OrderRule.InsufficientQuota"]           = "D2",
        ["StackRule.EvenBayInvalidFor20Ft"]       = "Y1",
        ["YardRule.InvalidBayForSize"]            = "Y1",
        ["StackRule.SlotBelowEmpty"]              = "Y4",
        ["YardRule.SlotBelowNotOccupied"]         = "Y4",
        ["YardSlot.CannotRemoveWithContainersAbove"] = "Y5",
        ["StackRule.ContainerAboveMustBeEmpty"]   = "Y5",
        ["G5.DirectShipNotAllowed"]               = "G5",
        ["G9.SlotOccupied"]                       = "G9",
        ["YardBlock.CannotShrinkOccupiedSlots"]   = "Y6",
        ["YardBlock.VirtualCannotShrink"]         = "Y6a",
    };

    public static string? GetRuleCode(string errorCode) =>
        _map.TryGetValue(errorCode, out var rc) ? rc : null;
}
