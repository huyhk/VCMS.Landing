using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace LandingCms.Services;

public static class DatabaseJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static string Normalize(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, Options);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
