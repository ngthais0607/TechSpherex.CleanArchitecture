using Microsoft.AspNetCore.WebUtilities;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Api.Extensions;

public static class ResultExtensions
{
    private static readonly IReadOnlyDictionary<string, int> StatusCodesByError = BuildMap();

    private static Dictionary<string, int> BuildMap()
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        void Add(int status, string codes)
        {
            foreach (var code in codes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) map.Add(code, status);
        }
        Add(400, "Validation.Failed Request.Invalid Request.InvalidPagination Container.EmptyNumber Container.InvalidCondition Container.InvalidMaxGrossWeight Container.InvalidTareWeight User.EmptyEmail User.EmptyUserName User.InvalidTenant Role.EmptyName Customer.EmptyName Customer.EmptyTaxCode Customer.InvalidTaxCodeFormat Customer.InvalidTaxCodeLength LineOperator.EmptyCode LineOperator.EmptyName LineOperator.InvalidOwnerCode Vehicle.EmptyPlateNumber Voyage.EmptyVesselName Voyage.EmptyVoyageNumber DeliveryOrder.EmptyCustomerName DeliveryOrder.EmptyCustomerTaxCode DeliveryOrder.EmptyLineOperator DeliveryOrder.EmptyOrderNumber DeliveryOrder.EmptyOutboundVessel DeliveryOrder.InvalidQuantity DeliveryOrderItem.InvalidQuantity ContainerNumber.CheckDigitMismatch ContainerNumber.Empty ContainerNumber.InvalidCategoryIdentifier ContainerNumber.InvalidCheckDigit ContainerNumber.InvalidLength ContainerNumber.InvalidOwnerCode ContainerNumber.InvalidSerialNumber YardPosition.EmptyBlock YardPosition.InvalidCoordinate Container.InvalidIsoCode Container.IsoSizeMismatch Container.FutureManufactureDate OrderRule.UnknownLineOperator OrderRule.UnknownCustomer OrderRule.ExpiryInPast OrderRule.DuplicateLine G5.DirectShipNotAllowed Booking.OrderExpired Booking.InvalidData");
        Add(401, "Auth.Unauthorized User.NotFound User.InvalidCredentials Auth.RefreshTokenExpired Auth.InvalidRefreshToken");
        Add(403, "Auth.Forbidden Tenant.Invalid Tenant.Mismatch");
        Add(404, "Container.NotFound DeliveryOrder.NotFound YardBlock.NotFound YardSlot.NotFound YardSlot.SlotNotOccupied LineOperator.NotFound LineOperator.OwnerCodeNotFound Booking.NotFound GateTransaction.NotFound Customer.NotFound");
        Add(409, "Conflict.ConcurrentUpdate Conflict.Duplicate Container.AlreadyExists Customer.AlreadyExists LineOperator.AlreadyExists LineOperator.OwnerCodeAlreadyExists Vehicle.AlreadyExists Voyage.AlreadyExists DeliveryOrder.AlreadyExists User.AlreadyExists YardBlock.AlreadyExists Lifecycle.ContainerAlreadyInYard Lifecycle.AlreadyGatedOut Lifecycle.NotInYard YardSlot.SlotAlreadyOccupied YardSlot.ContainerAlreadyPlaced YardRule.SlotAlreadyOccupied Booking.ContainerAlreadyReserved GateOut.WrongOrderReservation GateTransaction.AlreadyCancelled GateTransaction.CancelledCannotUpdate G9.SlotOccupied Customer.HasActiveOrders LineOperator.HasActiveResources");
        Add(422, "YardBlock.CannotPlaceInVirtual YardSlot.CannotRemoveWithContainersAbove YardBlock.CannotShrinkOccupiedSlots YardBlock.InvalidExpansion YardBlock.InvalidShrink YardBlock.VirtualCannotExpand YardBlock.VirtualCannotShrink DeliveryOrder.AlreadyCompleted DeliveryOrderItem.QuantityExceeded Booking.QuantityExceeded Booking.QuotaExceeded OrderRule.InsufficientQuota OrderRule.LineOperatorMismatch LifecycleRule.ContainerNotInYard OrderRule.NoRemainingQuantity OrderRule.DeliveryOrderExpired YardRule.SlotBelowNotOccupied YardRule.SlotNotFound YardRule.TierExceedsBlockLimit YardRule.InvalidBayForSize YardRule.StackIncompatible StackRule.EvenBayInvalidFor20Ft StackRule.SlotBelowEmpty StackRule.ContainerAboveMustBeEmpty");
        Add(409, "YardSlot.InvalidFootprint");
        Add(400, "Movement.InvalidData");
        Add(429, "RateLimit.TooManyRequests");
        return map;
    }

    public static IResult ToHttpResult(this Result result) => result.IsSuccess
        ? Results.NoContent() : FromError(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess ? (onSuccess is null ? Results.Ok(result.Value) : onSuccess(result.Value!)) : FromError(result.Error);

    public static IResult FromError(Error error)
    {
        var status = StatusCodesByError.GetValueOrDefault(error.Code, 500);
        // Authentication failures do not reveal whether an account exists.
        return Problem(status, status == 401 ? "Auth.Unauthorized" : status == 500 ? "Server.Unexpected" : error.Code,
            status == 401 ? "Authentication failed." : status == 500 ? "An unexpected error occurred." : error.Description,
            unknownError: status == 500 ? error.Code : null);
    }

    public static IResult Problem(int status, string code, string? detail = null,
        IDictionary<string, string[]>? errors = null, string? unknownError = null) =>
        new ProblemResult(status, code, detail, errors, unknownError);

    private sealed record ProblemResult(int Status, string Code, string? Detail,
        IDictionary<string, string[]>? Errors, string? UnknownError) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            if (UnknownError is not null)
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ResultExtensions")
                    .LogError("Unmapped application error code {ErrorCode}", UnknownError);
            var extensions = new Dictionary<string, object?>
                { ["code"] = Code, ["traceId"] = context.TraceIdentifier };
            var ruleCode = RuleCodeMap.GetRuleCode(Code);
            if (ruleCode is not null) extensions["ruleCode"] = ruleCode;
            if (Errors is not null) extensions["errors"] = Errors;
            return Results.Problem(statusCode: Status, title: ReasonPhrases.GetReasonPhrase(Status),
                type: "about:blank", detail: Detail,
                extensions: extensions).ExecuteAsync(context);
        }
    }
}
