using LandingCms.Data;
using LandingCms.Models;
using LandingCms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LandingCms.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "SuperAdministrator,Administrator")]
public class ContactsController(
    ApplicationDbContext db,
    IContactEmailSender emailSender,
    ILogger<ContactsController> logger) : Controller
{
    public async Task<IActionResult> Index() => View(await db.ContactSubmissions.AsNoTracking()
        .OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync());

    public async Task<IActionResult> Detail(long id) =>
        await db.ContactSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } item
            ? View(item)
            : NotFound();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(long id)
    {
        var submission = await db.ContactSubmissions.FirstOrDefaultAsync(x => x.Id == id);
        if (submission is null) return NotFound();

        if (submission.Status == "Sent")
        {
            TempData["Message"] = "Email này đã được gửi trước đó nên hệ thống không gửi trùng.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var sent = await TrySendAsync(submission);
        await db.SaveChangesAsync();
        if (sent) TempData["Message"] = "Đã gửi lại email liên hệ thành công.";
        else TempData["Error"] = "Gửi lại email thất bại. Vui lòng xem lỗi mới nhất trong chi tiết liên hệ.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendAll()
    {
        var submissions = await db.ContactSubmissions
            .Where(x => x.Status != "Sent")
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        var sentCount = 0;
        var failedCount = 0;
        foreach (var submission in submissions)
        {
            if (await TrySendAsync(submission)) sentCount++;
            else failedCount++;

            // Lưu từng kết quả để các email đã gửi không bị gửi lại nếu request bị gián đoạn.
            await db.SaveChangesAsync();
        }

        var resultMessage = submissions.Count == 0
            ? "Không có email thất bại hoặc đang chờ gửi."
            : $"Đã xử lý {submissions.Count} email: {sentCount} thành công, {failedCount} thất bại.";
        if (failedCount > 0) TempData["Error"] = resultMessage;
        else TempData["Message"] = resultMessage;
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TrySendAsync(ContactSubmission submission)
    {
        try
        {
            var recipient = (await db.SiteSettings.AsNoTracking().FirstAsync()).Email ?? "";
            await emailSender.SendAsync(submission, recipient);
            submission.Status = "Sent";
            submission.SentAtUtc = DateTime.UtcNow;
            submission.ErrorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            submission.Status = "Failed";
            submission.SentAtUtc = null;
            submission.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 1000)];
            logger.LogError(ex, "Could not resend contact submission {SubmissionId}", submission.Id);
            return false;
        }
    }
}
