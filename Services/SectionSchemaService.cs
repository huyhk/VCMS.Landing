using System.Text.Json;

namespace LandingCms.Services;

public sealed class SectionFieldSchema
{
    public string Editor { get; set; } = "textarea";
    public string? HtmlPolicy { get; set; }
}

public sealed class SectionSettingOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}

public sealed class SectionSettingSchema
{
    public string Editor { get; set; } = "select";
    public string? Default { get; set; }
    public List<SectionSettingOption> Options { get; set; } = new();
}

public sealed class SectionSchemaDocument
{
    public Dictionary<string, SectionFieldSchema> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SectionSettingSchema> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface ISectionSchemaService
{
    SectionFieldSchema GetField(string? schemaJson, string fieldName);
    SectionSettingSchema? GetSetting(string? schemaJson, string settingName);
    string? ResolveSetting(string? schemaJson, string? settingsJson, string settingName);
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

    public SectionSettingSchema? GetSetting(string? schemaJson, string settingName)
    {
        var schema = ParseSchema(schemaJson);
        return schema?.Settings.FirstOrDefault(x => string.Equals(x.Key, settingName, StringComparison.OrdinalIgnoreCase)).Value;
    }

    public string? ResolveSetting(string? schemaJson, string? settingsJson, string settingName)
    {
        var definition = GetSetting(schemaJson, settingName);
        if (definition is null) return null;
        string? value = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(settingsJson))
            {
                using var document = JsonDocument.Parse(settingsJson);
                if (document.RootElement.TryGetProperty(settingName, out var property) && property.ValueKind == JsonValueKind.String)
                    value = property.GetString();
            }
        }
        catch (JsonException) { }
        return definition.Options.Any(x => x.Value == value) ? value : definition.Default;
    }

    private static SectionSchemaDocument? ParseSchema(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson)) return null;
        try { return JsonSerializer.Deserialize<SectionSchemaDocument>(schemaJson, JsonOptions); }
        catch (JsonException) { return null; }
    }
}
