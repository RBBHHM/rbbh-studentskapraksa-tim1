using System.Text.Json;
using RBBH.ConnectedParties.API.Controllers;

namespace IntegrationTests.Tests.Operations;

public sealed class BusinessMessageEncodingTests
{
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
