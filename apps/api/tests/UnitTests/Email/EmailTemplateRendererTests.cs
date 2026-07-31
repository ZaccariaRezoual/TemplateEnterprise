using EnterpriseFramework.Modules.Email.Services;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Email;

public sealed class EmailTemplateRendererTests
{
    private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
    {
        ["name"] = "Ada",
        ["application"] = "Framework",
    };

    [Fact]
    public void RenderHtml_SubstitutesPlaceholders()
    {
        var result = EmailTemplateRenderer.RenderHtml("<p>Hi {{name}}</p>", Values);

        result.ShouldBe("<p>Hi Ada</p>");
    }

    [Fact]
    public void RenderHtml_EncodesSubstitutedValues()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = "<script>alert(1)</script>",
        };

        var result = EmailTemplateRenderer.RenderHtml("<p>Hi {{name}}</p>", values);

        // A display name reaches this template: unencoded output would make
        // the email a script-injection vector in web mail clients.
        result.ShouldNotContain("<script>");
        result.ShouldContain("&lt;script&gt;");
    }

    [Fact]
    public void RenderText_DoesNotEncode()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal) { ["name"] = "A & B" };

        var result = EmailTemplateRenderer.RenderText("Hi {{name}}", values);

        result.ShouldBe("Hi A & B");
    }

    [Fact]
    public void Render_LeavesUnknownPlaceholdersVisible()
    {
        var result = EmailTemplateRenderer.RenderText("Hi {{missing}}", Values);

        // Visibly broken gets reported; silently blank does not.
        result.ShouldBe("Hi {{missing}}");
    }

    [Fact]
    public void Render_SubstitutesEveryOccurrence()
    {
        var result = EmailTemplateRenderer.RenderText("{{application}} — {{application}}", Values);

        result.ShouldBe("Framework — Framework");
    }
}
