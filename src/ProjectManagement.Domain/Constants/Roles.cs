namespace ProjectManagement.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";

    public static readonly string[] AllRoles = { Admin, Manager, User };

    public static bool IsValidRole(string role)
    {
        return AllRoles.Contains(role);
    }
}
