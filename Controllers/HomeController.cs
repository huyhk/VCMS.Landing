using LandingCms.Data;
using LandingCms.Models;
using LandingCms.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LandingCms.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace LandingCms.Controllers;
public class HomeController(ApplicationDbContext db, IContactEmailSender emailSender, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstAsync();
        var templateSetting = await db.SiteTemplateSettings.AsNoTracking()
            .Include(x => x.ActiveTemplate).FirstAsync();
        var slots = await db.TemplateSections.AsNoTracking()
            .Include(x => x.SectionDefinition)
            .Where(x => x.TemplateId == templateSetting.ActiveTemplateId)
            .OrderBy(x => x.SortOrder).ToListAsync();
        var keys = slots.Select(x => x.SectionKey).ToArray();
        var contents = await db.SectionContents.AsNoTracking()
            .Where(x => keys.Contains(x.SectionKey) && x.IsPublished)
            .ToDictionaryAsync(x => x.SectionKey);
        var sections = new List<LandingSection>();
        foreach (var slot in slots)
        {
            if (!contents.TryGetValue(slot.SectionKey, out var content)) continue;
            var payload = JsonSerializer.Deserialize<SectionContentPayload>(content.ContentJson) ?? new();
            sections.Add(new LandingSection
            {
                SectionKey = slot.SectionKey, SectionType = slot.SectionDefinition.SectionType,
                Eyebrow = payload.Eyebrow, Title = payload.Title, Subtitle = payload.Subtitle,
                Content = payload.Content, ImageUrl = payload.ImageUrl,
                PrimaryButtonText = payload.PrimaryButtonText, PrimaryButtonUrl = payload.PrimaryButtonUrl,
                SecondaryButtonText = payload.SecondaryButtonText, SecondaryButtonUrl = payload.SecondaryButtonUrl,
                SortOrder = slot.SortOrder, IsPublished = true
            });
        }
        var viewPath = templateSetting.ActiveTemplate.ViewPath;
        if (!viewPath.StartsWith("~/Views/Templates/", StringComparison.Ordinal) ||
            !viewPath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) || viewPath.Contains(".."))
            return Problem("Template view path is invalid.");
        return View(viewPath, new HomeViewModel(settings, sections));
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("contact")]
    public async Task<IActionResult> Contact(ContactFormViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Website)) return Redirect("/#contact");
        if (!ModelState.IsValid) { TempData["ContactError"] = "Vui lòng kiểm tra lại thông tin liên hệ."; return Redirect("/#contact"); }
        var submission = new ContactSubmission
        {
            Name = model.Name.Trim(), Email = model.Email.Trim(), Phone = model.Phone?.Trim(), Message = model.Message.Trim(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()[..Math.Min(Request.Headers.UserAgent.ToString().Length, 500)]
        };
        db.ContactSubmissions.Add(submission);
        await db.SaveChangesAsync();
        try
        {
            var recipient = (await db.SiteSettings.AsNoTracking().FirstAsync()).Email ?? "";
            await emailSender.SendAsync(submission, recipient);
            submission.Status = "Sent"; submission.SentAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            submission.Status = "Failed"; submission.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 1000)];
            logger.LogError(ex, "Could not send contact submission {SubmissionId}", submission.Id);
        }
        await db.SaveChangesAsync();
        TempData["ContactSuccess"] = "Cảm ơn bạn. Chúng tôi đã nhận được thông tin và sẽ liên hệ sớm.";
        return Redirect("/#contact");
    }
    public IActionResult Error() => View();
}
