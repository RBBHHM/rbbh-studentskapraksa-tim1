using System.Net;
using System.Text.Json;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.Services;

namespace IntegrationTests.Tests.Operations;

public sealed class BusinessMessageEncodingTests
{
    [Theory]
    [InlineData(0, "/app/admin/period")]
    [InlineData(1, "/app/notifications")]
    [InlineData(2, "/app/admin/period")]
    [InlineData(3, "/app/physical-persons")]
    [InlineData(4, "/app/physical-persons")]
    [InlineData(5, "/app/notifications")]
    [InlineData(6, "/app/admin/period")]
    [InlineData(7, "/app/admin/period")]
    public void AllMailTemplatesEncodeBusinessInputAndUseCanonicalAuthenticatedRoutes(int template, string route)
    {
        const string input = "<img src=x onerror=\"alert(1)\"> & ž";
        const string origin = "https://app.example.test";
        var html = template switch
        {
            0 => EmailTemplates.UnlockRequest(input, 2026, 10, input, origin),
            1 => EmailTemplates.NeedsInfo(input, 2026, 10, input, origin),
            2 => EmailTemplates.UserResponse(input, 2026, 10, input, origin),
            3 => EmailTemplates.HrNewPhysicalPerson(input, input, input, null, null, origin),
            4 => EmailTemplates.HrPhysicalPersonExpired(input, input, input, DateTime.UtcNow, origin),
            5 => EmailTemplates.UnlockConfirmation(input, 2026, 10, origin),
            6 => EmailTemplates.PeriodLocked(input, 2026, 10, origin),
            _ => EmailTemplates.PeriodUnlocked(input, 2026, 10, origin)
        };
        Assert.DoesNotContain(input, html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains(WebUtility.HtmlEncode(input), html);
        Assert.Contains(input, WebUtility.HtmlDecode(html));
        Assert.Contains($"href=\"{origin}{route}\"", html);
    }

    public static TheoryData<int> ControlCharacters => new(Enumerable.Range(0, 32));

    [Theory]
    [MemberData(nameof(ControlCharacters))]
    public void PeriodValidationMessageAlwaysProducesValidLosslessJson(int character)
    {
        var message = $"Početak ž {Convert.ToChar(character)} \\\" kraj";
        var json = JsonSerializer.Serialize(new ErrField("message", message));
        using var parsed = JsonDocument.Parse(json);
        Assert.Equal(message, parsed.RootElement.GetProperty("Message").GetString());
        Assert.Contains("Početak ž", json);
    }
}
