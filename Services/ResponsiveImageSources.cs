using LandingCms.Models;
using Microsoft.AspNetCore.Mvc;

namespace LandingCms.Services;

public sealed record ResponsiveImageSource(string Src, string SrcSet);

public static class ResponsiveImageSources
{
    public static ResponsiveImageSource Build(
        IUrlHelper url,
        MediaAsset asset,
        string profile,
        int preferredWidth)
    {
        var widths = profile == "hero" ? ImageVariants.HeroWidths : ImageVariants.ThumbnailWidths;
        var action = profile == "hero" ? "HeroVariant" : "ThumbnailVariant";
        var actualWidth = asset.Width.GetValueOrDefault();
        var candidates = widths
            .Where(width => actualWidth <= 0 || width <= actualWidth)
            .Select(width => new Candidate(
                width,
                url.Action(action, "Media", new { id = asset.Id, width }) ?? asset.RelativeUrl))
            .ToList();

        // When the source is smaller than the largest generated size, retain its true
        // intrinsic width as the final candidate instead of upscaling it.
        if (actualWidth > 0 && actualWidth < widths[^1] && candidates.All(x => x.Width != actualWidth))
            candidates.Add(new Candidate(actualWidth, asset.RelativeUrl));

        if (candidates.Count == 0)
            candidates.Add(new Candidate(actualWidth > 0 ? actualWidth : preferredWidth, asset.RelativeUrl));

        candidates = candidates.OrderBy(x => x.Width).ToList();
        var fallback = candidates.FirstOrDefault(x => x.Width >= preferredWidth) ?? candidates[^1];
        return new ResponsiveImageSource(
            fallback.Url,
            string.Join(", ", candidates.Select(x => $"{x.Url} {x.Width}w")));
    }

    private sealed record Candidate(int Width, string Url);
}
