using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LandingCms.Services;

public sealed class AiDraftStore(IMemoryCache cache, IOptions<AiContentOptions> options) : IAiDraftStore
{
    private string Key(string token, string userId) => $"ai-content-draft:{userId}:{token}";

    public void Set(AiLandingDraft draft) => cache.Set(Key(draft.Token, draft.UserId), draft,
        TimeSpan.FromMinutes(Math.Clamp(options.Value.DraftLifetimeMinutes, 10, 240)));

    public AiLandingDraft? Get(string token, string userId) =>
        cache.TryGetValue(Key(token, userId), out AiLandingDraft? draft) ? draft : null;

    public void Remove(string token, string userId) => cache.Remove(Key(token, userId));
}
