using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LandingCms.Services;

public sealed class OpenAiContentService(HttpClient http, IOptions<AiContentOptions> options, ILogger<OpenAiContentService> logger) : IAiContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly AiContentOptions _options = options.Value;

    public bool IsAvailable => _options.IsAvailable;
    public string ModelName => _options.DefaultModel;

    public async Task<AiLandingDraft> GenerateLandingAsync(AiContentRequest request, CancellationToken cancellationToken)
    {
        if (!IsAvailable) throw new InvalidOperationException("AI Content Studio chưa được cấu hình cho website này.");
        var payload = new
        {
            model = _options.DefaultModel,
            reasoning = new { effort = "low" },
            max_output_tokens = 12000,
            instructions = BuildInstructions(request),
            input = BuildBrief(request),
            text = new { format = new { type = "json_schema", name = "vcms_landing_draft", strict = true, schema = BuildOutputSchema() } }
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenAI content request failed with status {StatusCode}: {Response}", (int)response.StatusCode, Limit(body, 1200));
            throw new InvalidOperationException($"Không thể tạo nội dung AI (HTTP {(int)response.StatusCode}). Vui lòng kiểm tra cấu hình hoặc thử lại.");
        }
        try
        {
            using var document = JsonDocument.Parse(body);
            var outputText = ExtractOutputText(document.RootElement);
            var result = JsonSerializer.Deserialize<AiResponse>(outputText, JsonOptions)
                ?? throw new JsonException("AI trả về nội dung rỗng.");
            return Normalize(result, request);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Could not parse OpenAI structured output.");
            throw new InvalidOperationException("AI trả về dữ liệu không đúng định dạng VCMS. Vui lòng thử lại.");
        }
    }

    private static string BuildInstructions(AiContentRequest request)
    {
        var sectionLines = request.Sections.Select(section =>
        {
            var fields = string.Join(", ", section.Fields.Select(x => $"{x.Key} ({x.Value.Editor})"));
            var items = string.Join(", ", section.ItemFields.Select(x => $"{x.Key} ({x.Value.Editor}{(x.Value.Required ? ", bắt buộc" : "")})"));
            return $"- {section.SectionKey}: {section.DisplayName}; type={section.SectionType}; contentFields=[{fields}]; itemFields=[{items}]";
        });
        return $"""
            Bạn là chuyên gia nội dung landing page và phải tạo bản nháp bằng {request.LanguageName}.
            Chỉ trả về các sectionKey được cung cấp. Tạo nội dung riêng, súc tích, hướng đến chuyển đổi và không lặp ý.
            Tuyệt đối không bịa đặt chứng nhận, khách hàng, số liệu, giá, bảo hành, địa chỉ hoặc cam kết chưa có trong brief.
            Với Testimonials, nếu brief không cung cấp phản hồi thật thì để items rỗng và ghi chú cần bổ sung phản hồi đã được cho phép sử dụng.
            URL nút chỉ dùng anchor của section hiện có (ví dụ #contact), URL hợp lệ do brief cung cấp, tel: hoặc mailto:.
            Field html chỉ dùng các thẻ đơn giản: p, ul, ol, li, strong, em, br, a. Không tạo script, style, iframe hoặc event handler.
            Field image phải để null vì giai đoạn này không tạo media. Mọi field không phù hợp phải để null.
            Mỗi section item tối đa 8 mục. Không đặt markdown vào field HTML.

            Cấu trúc template:
            {string.Join("\n", sectionLines)}
            """;
    }

    private static string BuildBrief(AiContentRequest request) => JsonSerializer.Serialize(new
    {
        request.Brief.Topic, request.Brief.BrandName, request.Brief.ProductOrService,
        request.Brief.TargetAudience, request.Brief.Differentiators, request.Brief.ServiceArea,
        request.Brief.ConversionGoal, request.Brief.Tone, request.Brief.AdditionalInstructions,
        language = request.LanguageCode,
        availableAnchors = request.Sections.Select(x => $"#{x.SectionKey}").ToArray()
    }, JsonOptions);

    private static object BuildOutputSchema() => new
    {
        type = "object",
        additionalProperties = false,
        properties = new
        {
            title = new { type = "string" },
            summary = new { type = "string" },
            sections = new
            {
                type = "array",
                items = new
                {
                    type = "object", additionalProperties = false,
                    properties = new
                    {
                        sectionKey = new { type = "string" },
                        fields = new { type = "array", items = FieldSchema() },
                        items = new { type = "array", items = new { type = "object", additionalProperties = false, properties = new { fields = new { type = "array", items = FieldSchema() } }, required = new[] { "fields" } } },
                        notes = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "sectionKey", "fields", "items", "notes" }
                }
            }
        },
        required = new[] { "title", "summary", "sections" }
    };

    private static object FieldSchema() => new
    {
        type = "object", additionalProperties = false,
        properties = new { key = new { type = "string" }, value = new { type = new[] { "string", "null" } } },
        required = new[] { "key", "value" }
    };

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var direct) && direct.ValueKind == JsonValueKind.String)
            return direct.GetString() ?? "";
        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
            foreach (var item in output.EnumerateArray())
                if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                    foreach (var part in content.EnumerateArray())
                        if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                            part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                            return text.GetString() ?? "";
        throw new JsonException("Response does not contain output_text.");
    }

    private static AiLandingDraft Normalize(AiResponse result, AiContentRequest request)
    {
        var specs = request.Sections.ToDictionary(x => x.SectionKey, StringComparer.OrdinalIgnoreCase);
        var draft = new AiLandingDraft
        {
            LanguageCode = request.LanguageCode,
            Title = string.IsNullOrWhiteSpace(result.Title) ? "Bản nháp nội dung landing page" : Limit(result.Title, 200),
            Summary = Limit(result.Summary, 1000)
        };
        foreach (var section in result.Sections)
        {
            if (!specs.TryGetValue(section.SectionKey, out var spec) || draft.Sections.Any(x => x.SectionKey.Equals(spec.SectionKey, StringComparison.OrdinalIgnoreCase))) continue;
            var content = FilterFields(section.Fields, spec.Fields);
            var items = section.Items.Take(8).Select(x => FilterFields(x.Fields, spec.ItemFields))
                .Where(x => x.Count > 0).ToList();
            draft.Sections.Add(new AiSectionDraft { SectionKey = spec.SectionKey, DisplayName = spec.DisplayName, SectionType = spec.SectionType, Content = content, Items = items, Notes = section.Notes.Select(x => Limit(x, 300)).Where(x => x.Length > 0).Take(5).ToList() });
        }
        foreach (var spec in request.Sections.Where(x => draft.Sections.All(y => !y.SectionKey.Equals(x.SectionKey, StringComparison.OrdinalIgnoreCase))))
            draft.Sections.Add(new AiSectionDraft { SectionKey = spec.SectionKey, DisplayName = spec.DisplayName, SectionType = spec.SectionType, Notes = ["AI chưa đề xuất nội dung cho section này."] });
        return draft;
    }

    private static Dictionary<string, string?> FilterFields(IEnumerable<AiField> fields, IReadOnlyDictionary<string, SectionFieldSchema> allowed)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            var allowedField = allowed.FirstOrDefault(x => x.Key.Equals(field.Key, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(allowedField.Key) || allowedField.Value.Editor == "image") continue;
            result[allowedField.Key] = field.Value is null ? null : Limit(field.Value.Trim(), MaxLength(allowedField.Key, allowedField.Value));
        }
        return result;
    }

    private static string Limit(string? value, int max) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static int MaxLength(string key, SectionFieldSchema field) => key.ToLowerInvariant() switch
    {
        "eyebrow" => 100,
        "title" => 200,
        "subtitle" => 300,
        "primarybuttontext" or "secondarybuttontext" or "buttontext" => 80,
        "primarybuttonurl" or "secondarybuttonurl" or "buttonurl" or "url" => 300,
        _ when field.Editor == "html" => 20000,
        _ => 4000
    };
    private sealed class AiResponse { public string Title { get; set; } = ""; public string Summary { get; set; } = ""; public List<AiResponseSection> Sections { get; set; } = []; }
    private sealed class AiResponseSection { public string SectionKey { get; set; } = ""; public List<AiField> Fields { get; set; } = []; public List<AiResponseItem> Items { get; set; } = []; public List<string> Notes { get; set; } = []; }
    private sealed class AiResponseItem { public List<AiField> Fields { get; set; } = []; }
    private sealed class AiField { public string Key { get; set; } = ""; public string? Value { get; set; } }
}
