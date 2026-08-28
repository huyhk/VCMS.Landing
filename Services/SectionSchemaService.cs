using System.Text.Json;

namespace LandingCms.Services;

public sealed class SectionFieldSchema
{
    public string Editor { get; set; } = "textarea";
    public string? HtmlPolicy { get; set; }
}

public sealed class SectionSchemaDocument
{
    public Dictionary<string, SectionFieldSchema> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface ISectionSchemaService
{
    SectionFieldSchema GetField(string? schemaJson, string fieldName);
}

public sealed class SectionSchemaService : ISectionSchemaService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SectionFieldSchema GetField(string? schemaJson, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(schemaJson)) return new();
        try
        {
            var schema = JsonSerializer.Deserialize<SectionSchemaDocument>(schemaJson, JsonOptions);
            return schema?.Fields.FirstOrDefault(x => string.Equals(x.Key, fieldName, StringComparison.OrdinalIgnoreCase)).Value ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
