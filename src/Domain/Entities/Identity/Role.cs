using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.Identity;

public sealed class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Role() { }

    public static Result<Role> Create(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(
                Error.Validation("Role.EmptyName", "Tên vai trò không được để trống."));
        }

        return Result.Success(new Role
        {
            Name = name.Trim(),
            Description = description.Trim()
        });
    }
}
