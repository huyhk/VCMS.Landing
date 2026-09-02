namespace LandingCms.Services;

public sealed class LicensingOptions
{
    public const string SectionName = "Licensing";
    public string ServerUrl { get; set; } = "";
    public string ProductCode { get; set; } = "VCMS.LANDING";
    public string LicenseKey { get; set; } = "";
    public string CanonicalHost { get; set; } = "";
    public int RefreshIntervalHours { get; set; } = 24;
    public int GracePeriodHours { get; set; } = 168;
    public int RequestTimeoutSeconds { get; set; } = 10;
    public bool BypassInDevelopment { get; set; } = true;
}

