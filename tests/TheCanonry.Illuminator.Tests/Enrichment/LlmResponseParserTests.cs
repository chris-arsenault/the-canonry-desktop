using System.Text.Json;
using TheCanonry.Illuminator.Enrichment;

namespace TheCanonry.Illuminator.Tests.Enrichment;

public sealed class LlmResponseParserTests
{
    private sealed record TestPayload
    {
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }

    [Fact]
    public void ExtractJson_RawJson()
    {
        var json = """{"name": "test", "value": 42}""";
        var result = LlmResponseParser.ExtractJson<TestPayload>(json);
        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ExtractJson_CodeBlock()
    {
        var response = """
            Here is the result:
            ```json
            {"name": "block", "value": 7}
            ```
            That's the output.
            """;
        var result = LlmResponseParser.ExtractJson<TestPayload>(response);
        Assert.Equal("block", result.Name);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void ExtractJson_GenericCodeBlock()
    {
        var response = """
            ```
            {"name": "generic", "value": 3}
            ```
            """;
        var result = LlmResponseParser.ExtractJson<TestPayload>(response);
        Assert.Equal("generic", result.Name);
    }

    [Fact]
    public void ExtractJson_JsonInProse()
    {
        var response = """
            I analyzed the data and here are my findings:

            The result is {"name": "prose", "value": 99} which shows significant progress.
            """;
        var result = LlmResponseParser.ExtractJson<TestPayload>(response);
        Assert.Equal("prose", result.Name);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public void ExtractJson_TrailingCommas()
    {
        var json = """{"name": "trailing", "value": 5,}""";
        var result = LlmResponseParser.ExtractJson<TestPayload>(json);
        Assert.Equal("trailing", result.Name);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void ExtractJson_Array()
    {
        var json = """["one", "two", "three"]""";
        var result = LlmResponseParser.ExtractJson<List<string>>(json);
        Assert.Equal(3, result.Count);
        Assert.Equal("two", result[1]);
    }

    [Fact]
    public void ExtractJson_EmptyInput_Throws()
    {
        Assert.Throws<JsonException>(() => LlmResponseParser.ExtractJson<TestPayload>(""));
    }

    [Fact]
    public void ExtractJson_NoJsonInText_Throws()
    {
        Assert.Throws<JsonException>(() =>
            LlmResponseParser.ExtractJson<TestPayload>("This is just plain text with no JSON."));
    }

    [Fact]
    public void TryExtractJson_Success_ReturnsTrue()
    {
        var json = """{"name": "try", "value": 1}""";
        var success = LlmResponseParser.TryExtractJson<TestPayload>(json, out var result);
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal("try", result.Name);
    }

    [Fact]
    public void TryExtractJson_Failure_ReturnsFalse()
    {
        var success = LlmResponseParser.TryExtractJson<TestPayload>("not json", out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractJson_NestedObjects()
    {
        var response = """
            Sure, here's the data:
            {"name": "nested", "value": 10, "extra": {"a": 1, "b": [1,2,3]}}
            Hope that helps!
            """;
        // Should extract the full JSON including nested content
        var raw = LlmResponseParser.ExtractJson(response);
        using var doc = JsonDocument.Parse(raw);
        Assert.Equal("nested", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(10, doc.RootElement.GetProperty("value").GetInt32());
    }

    [Fact]
    public void ExtractJson_TrailingCommaInArray()
    {
        var json = """["a", "b", "c",]""";
        var result = LlmResponseParser.ExtractJson<List<string>>(json);
        Assert.Equal(3, result.Count);
    }
}
