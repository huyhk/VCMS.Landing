using System.Reflection;
using System.Text.Json;

namespace LandingCms.Services;

public sealed record DeploymentVersionInfo(string Version, bool IsManaged);

public interface IDeploymentVersionProvider
{
    DeploymentVersionInfo Current { get; }
}

public sealed class DeploymentVersionProvider : IDeploymentVersionProvider
{
    public DeploymentVersionProvider(IWebHostEnvironment environment)
    {
        Current = Read(environment.ContentRootPath, GetAssemblyVersion());
    }

    public DeploymentVersionInfo Current { get; }

    public static DeploymentVersionInfo Read(string contentRootPath, string fallbackVersion)
    {
        try
        {
            var markerPath = Path.Combine(contentRootPath, ".vns-deployment.json");
            if (File.Exists(markerPath))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(markerPath));
                if (document.RootElement.TryGetProperty("PackageVersion", out var property))
                {
                    var version = property.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(version))
                        return new DeploymentVersionInfo(version, true);
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }

        return new DeploymentVersionInfo(
            string.IsNullOrWhiteSpace(fallbackVersion) ? "Development" : fallbackVersion,
            false);
    }

    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(DeploymentVersionProvider).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+', 2)[0];
        return !string.IsNullOrWhiteSpace(informational)
            ? informational
            : assembly.GetName().Version?.ToString(3) ?? "Development";
    }
}
