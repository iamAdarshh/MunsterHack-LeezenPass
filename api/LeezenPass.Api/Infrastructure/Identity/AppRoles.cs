namespace LeezenPass.Api.Infrastructure.Identity;

/// <summary>ASP.NET Core Identity roles. Partner/admin endpoints require the role.</summary>
public static class AppRoles
{
  public const string Partner = "Partner";
  public const string Admin = "Admin";

  public static readonly IReadOnlyList<string> All = [Partner, Admin];
}
