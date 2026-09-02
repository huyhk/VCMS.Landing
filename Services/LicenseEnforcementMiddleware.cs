using System.Globalization;
using Microsoft.Extensions.Options;

namespace LandingCms.Services;

public sealed class LicenseEnforcementMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ILicenseValidationService validation,
        ILicenseState state,
        IOptions<LicensingOptions> options,
        IWebHostEnvironment environment)
    {
        await validation.InitializeAsync(context.RequestAborted);
        var snapshot = state.Current;
        if (snapshot.LocalStatus == LocalLicenseStatus.DevelopmentBypass && environment.IsDevelopment())
        {
            await next(context);
            return;
        }

        var requestHost = NormalizeHost(context.Request.Host.Host);
        var hostAllowed = snapshot.AllowedHosts.Any(x =>
            string.Equals(NormalizeHost(x), requestHost, StringComparison.OrdinalIgnoreCase));
        if (snapshot.AllowsLicensedHost && hostAllowed)
        {
            await next(context);
            return;
        }

        var hostMismatch = snapshot.AllowedHosts.Count > 0 && !hostAllowed;
        var isSafePublicRequest = HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method);
        var isAdminRequest = context.Request.Path.StartsWithSegments("/admin")
            || context.Request.Path.StartsWithSegments("/account");
        if (hostMismatch && isSafePublicRequest && !isAdminRequest
            && TryBuildRedirect(snapshot.CanonicalUrl, context.Request.PathBase, context.Request.Path, out var redirectUrl))
        {
            context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
            context.Response.Headers.Location = redirectUrl;
            return;
        }

        context.Response.StatusCode = hostMismatch || snapshot.LocalStatus == LocalLicenseStatus.Invalid
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(
            hostMismatch ? "Website đang chạy trên domain không được cấp phép."
                : snapshot.Message ?? "Không thể xác minh license của website.",
            context.RequestAborted);
    }

    private static string NormalizeHost(string host)
    {
        var value = host.Trim().TrimEnd('.').ToLowerInvariant();
        try { return new IdnMapping().GetAscii(value); }
        catch (ArgumentException) { return value; }
    }

    private static bool TryBuildRedirect(string? canonicalUrl, PathString pathBase, PathString path, out string redirectUrl)
    {
        redirectUrl = "";
        if (!Uri.TryCreate(canonicalUrl, UriKind.Absolute, out var canonical)
            || canonical.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(canonical.Host)) return false;
        var builder = new UriBuilder(canonical)
        {
            Path = pathBase.Add(path).Value ?? "/",
            Query = "",
            Fragment = ""
        };
        redirectUrl = builder.Uri.AbsoluteUri;
        return true;
    }
}

