namespace Book.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static string Label(string role) => role == Admin ? "管理員" : "一般使用者";
}
