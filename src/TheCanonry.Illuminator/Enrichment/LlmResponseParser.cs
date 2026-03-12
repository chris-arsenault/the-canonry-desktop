using System.Text.Json;
using System.Text.RegularExpressions;

namespace TheCanonry.Illuminator.Enrichment;

/// <summary>
/// Extracts JSON from LLM responses, handling common formatting quirks:
/// raw JSON, ```json code blocks, JSON embedded in prose, trailing commas.
/// </summary>
public static partial class LlmResponseParser
{
    /// <summary>
    /// Extract and deserialize JSON of type T from an LLM response string.
    /// </summary>
    public static T ExtractJson<T>(string response) where T : class
    {
        var json = ExtractJson(response);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new JsonException($"Deserialization of {typeof(T).Name} returned null");
    }

    /// <summary>
    /// Try to extract and deserialize JSON of type T from an LLM response.
    /// Returns false if extraction or deserialization fails.
    /// </summary>
    public static bool TryExtractJson<T>(string response, out T? result) where T : class
    {
        result = default;
        try
        {
            result = ExtractJson<T>(response);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract raw JSON string from an LLM response.
    /// Handles: raw JSON, ```json code blocks, JSON in prose.
    /// </summary>
    public static string ExtractJson(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new JsonException("Empty response");

        var trimmed = response.Trim();

        // Case 1: Raw JSON (starts with { or [)
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return CleanJsonQuirks(trimmed);

        // Case 2: ```json code block
        var codeBlockMatch = CodeBlockRegex().Match(trimmed);
        if (codeBlockMatch.Success)
            return CleanJsonQuirks(codeBlockMatch.Groups[1].Value.Trim());

        // Case 3: Generic ``` code block
        var genericBlockMatch = GenericBlockRegex().Match(trimmed);
        if (genericBlockMatch.Success)
        {
            var inner = genericBlockMatch.Groups[1].Value.Trim();
            if (inner.StartsWith('{') || inner.StartsWith('['))
                return CleanJsonQuirks(inner);
        }

        // Case 4: JSON embedded in prose — find first { or [ and match to closing
        var jsonStart = FindJsonStart(trimmed);
        if (jsonStart >= 0)
        {
            var extracted = ExtractBalancedJson(trimmed, jsonStart);
            if (extracted is not null)
                return CleanJsonQuirks(extracted);
        }

        throw new JsonException("No JSON found in response");
    }

    private static int FindJsonStart(string text)
    {
        var braceIdx = text.IndexOf('{');
        var bracketIdx = text.IndexOf('[');
        if (braceIdx < 0) return bracketIdx;
        if (bracketIdx < 0) return braceIdx;
        return Math.Min(braceIdx, bracketIdx);
    }

    private static string? ExtractBalancedJson(string text, int start)
    {
        var open = text[start];
        var close = open == '{' ? '}' : ']';
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (escape)
            {
                escape = false;
                continue;
            }
            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }
            if (c == '"')
            {
                inString = !inString;
                continue;
            }
            if (inString) continue;

            if (c == open) depth++;
            else if (c == close) depth--;

            if (depth == 0)
                return text[start..(i + 1)];
        }

        return null;
    }

    /// <summary>
    /// Clean common LLM quirks: trailing commas before closing brackets.
    /// </summary>
    private static string CleanJsonQuirks(string json)
    {
        // Remove trailing commas before } or ]
        return TrailingCommaRegex().Replace(json, "$1");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [GeneratedRegex(@"```json\s*([\s\S]*?)\s*```", RegexOptions.Compiled)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"```\s*([\s\S]*?)\s*```", RegexOptions.Compiled)]
    private static partial Regex GenericBlockRegex();

    [GeneratedRegex(@",\s*([}\]])", RegexOptions.Compiled)]
    private static partial Regex TrailingCommaRegex();
}
