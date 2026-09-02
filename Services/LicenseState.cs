namespace LandingCms.Services;

public enum LocalLicenseStatus { Unknown, Valid, GracePeriod, Invalid, Unavailable, DevelopmentBypass }

public sealed record LicenseSnapshot(
    LocalLicenseStatus LocalStatus,
    string ServerStatus,
    string? CanonicalUrl,
    IReadOnlyList<string> AllowedHosts,
    DateTime? LastOnlineCheckUtc,
    DateTime? NextCheckUtc,
    string? Message)
{
    public bool AllowsLicensedHost => LocalStatus is LocalLicenseStatus.Valid
        or LocalLicenseStatus.GracePeriod or LocalLicenseStatus.DevelopmentBypass;
}

public interface ILicenseState
{
    LicenseSnapshot Current { get; }
    void Set(LicenseSnapshot snapshot);
}

public sealed class LicenseState : ILicenseState
{
    private LicenseSnapshot current = new(LocalLicenseStatus.Unknown, "Unknown", null, [], null, null, "Chưa kiểm tra license.");
    public LicenseSnapshot Current => Volatile.Read(ref current);
    public void Set(LicenseSnapshot snapshot) => Volatile.Write(ref current, snapshot);
}

