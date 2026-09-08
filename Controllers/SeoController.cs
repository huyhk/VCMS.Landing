using System.Text;
using System.Xml;
using LandingCms.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VNS.Licensing.Client.AspNetCore;

namespace LandingCms.Controllers;

public sealed class SeoController(ApplicationDbContext db, ILicenseState licenseState) : Controller
{
    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ContentResult Robots()
    {
        var root = ResolveCanonicalRoot();
        return Content($"User-agent: *\nAllow: /\nDisallow: /admin\nSitemap: {root}sitemap.xml\n",
            "text/plain", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<ContentResult> Sitemap(CancellationToken ct)
    {
        var languages = await db.ContentLanguages.AsNoTracking()
            .Where(x => x.IsEnabled).OrderByDescending(x => x.IsDefault).ThenBy(x => x.SortOrder).ToListAsync(ct);
        var root = ResolveCanonicalRoot();
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, new XmlWriterSettings
        {
            OmitXmlDeclaration = true, Encoding = Encoding.UTF8, Indent = true
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var language in languages)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", language.IsDefault ? root : $"{root}{language.Code}/");
                writer.WriteElementString("changefreq", "weekly");
                writer.WriteElementString("priority", language.IsDefault ? "1.0" : "0.8");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return Content(builder.ToString(), "application/xml", Encoding.UTF8);
    }

    private string ResolveCanonicalRoot()
    {
        if (Uri.TryCreate(licenseState.Current.CanonicalUrl, UriKind.Absolute, out var canonical))
            return canonical.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/";
        return $"{Request.Scheme}://{Request.Host}/";
    }
}
