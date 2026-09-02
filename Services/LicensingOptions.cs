namespace LandingCms.Services;

public sealed class LicensingOptions
{
    public const string SectionName = "Licensing";
    public string ServerUrl { get; set; } = "";
    public string ProductCode { get; set; } = "VCMS.LANDING";
    public string LicenseKey { get; set; } = "";
    public bool BypassInDevelopment { get; set; } = true;
}
