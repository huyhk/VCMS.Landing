using LandingCms.Services;

namespace VCMS.Landing.Tests;

public sealed class DeploymentVersionProviderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"vcms-version-{Guid.NewGuid():N}");

    public DeploymentVersionProviderTests() => Directory.CreateDirectory(root);

    [Fact]
    public void Reads_package_version_from_deployment_marker()
    {
        File.WriteAllText(Path.Combine(root, ".vns-deployment.json"),
            """{"DeploymentId":"00000000-0000-0000-0000-000000000001","PackageVersion":"1.0.5","PackageSha256":"ABC"}""");

        var result = DeploymentVersionProvider.Read(root, "1.0.0");

        Assert.Equal("1.0.5", result.Version);
        Assert.True(result.IsManaged);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{not-json}")]
    [InlineData("""{"PackageVersion":""}""")]
    public void Falls_back_when_marker_is_missing_or_invalid(string? marker)
    {
        if (marker is not null)
            File.WriteAllText(Path.Combine(root, ".vns-deployment.json"), marker);

        var result = DeploymentVersionProvider.Read(root, "2.3.4");

        Assert.Equal("2.3.4", result.Version);
        Assert.False(result.IsManaged);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
