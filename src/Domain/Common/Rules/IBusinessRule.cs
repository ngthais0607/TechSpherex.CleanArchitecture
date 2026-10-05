namespace SnpDepotManagement.Domain.Common.Rules;

public interface IBusinessRule
{
    Error Error { get; }
    string Message => Error.Description;
    bool IsBroken();
}
