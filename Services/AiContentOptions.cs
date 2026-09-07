namespace LandingCms.Services;

public sealed class AiContentOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string DefaultModel { get; set; } = "gpt-5.6-terra";
    public int TimeoutSeconds { get; set; } = 120;
    public int DraftLifetimeMinutes { get; set; } = 60;

    public bool IsAvailable => Enabled &&
        Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(DefaultModel);
}
