using System.Text.Json;
using LandingCms.Services;

namespace LandingCms.Tests;

public sealed class ChromeLayoutServiceTests
{
    [Fact]
    public void Footer_component_formatting_is_preserved_and_normalized()
    {
        const string json = """
            {
              "rows": [{
                "key": "footer",
                "columns": [{
                  "components": [{
                    "type": "company",
                    "fontWeight": "bold",
                    "italic": true,
                    "fontSize": "large",
                    "textColor": "#AABBCC",
                    "textAlign": "center",
                    "spacingTop": "small",
                    "spacingBottom": "large"
                  }]
                }]
              }]
            }
            """;

        var normalized = new ChromeLayoutService().NormalizeFooter(json);
        var component = JsonSerializer.Deserialize<ChromeLayout>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web))!
            .Rows[0].Columns[0].Components[0];

        Assert.Equal("bold", component.FontWeight);
        Assert.True(component.Italic);
        Assert.Equal("large", component.FontSize);
        Assert.Equal("#aabbcc", component.TextColor);
        Assert.Equal("center", component.TextAlign);
        Assert.Equal("small", component.SpacingTop);
        Assert.Equal("large", component.SpacingBottom);
    }

    [Fact]
    public void Legacy_component_without_formatting_uses_safe_defaults()
    {
        const string json = """{"rows":[{"columns":[{"components":[{"type":"copyright"}]}]}]}""";

        var layout = new ChromeLayoutService().ParseFooter(json);
        var component = layout.Rows[0].Columns[0].Components[0];

        Assert.Equal("default", component.FontWeight);
        Assert.False(component.Italic);
        Assert.Equal("default", component.FontSize);
        Assert.Null(component.TextColor);
        Assert.Equal("default", component.TextAlign);
        Assert.Equal("none", component.SpacingTop);
        Assert.Equal("none", component.SpacingBottom);
    }

    [Fact]
    public void Unsupported_formatting_values_are_reset()
    {
        const string json = """
            {"rows":[{"columns":[{"components":[{
              "type":"text",
              "fontWeight":"900",
              "fontSize":"huge",
              "textColor":"red;position:fixed",
              "textAlign":"justify",
              "spacingTop":"100px",
              "spacingBottom":"-20px"
            }]}]}]}
            """;

        var layout = new ChromeLayoutService().ParseFooter(json);
        var component = layout.Rows[0].Columns[0].Components[0];

        Assert.Equal("default", component.FontWeight);
        Assert.Equal("default", component.FontSize);
        Assert.Null(component.TextColor);
        Assert.Equal("default", component.TextAlign);
        Assert.Equal("none", component.SpacingTop);
        Assert.Equal("none", component.SpacingBottom);
    }
}
