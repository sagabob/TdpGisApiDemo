namespace TdpGis.Endpoints.Options;

public class AdminDashboardOptions
{
    public const string SectionName = "AdminDashboard";

    /// <summary>Login name that must match for cookie sign-in.</summary>
    public string User { get; set; } = "admin";

    /// <summary>Plain password from configuration (use environment variables or user secrets in production).</summary>
    public string Password { get; set; } = string.Empty;
}