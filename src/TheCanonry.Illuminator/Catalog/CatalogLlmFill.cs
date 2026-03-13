using System.Text;
using System.Text.Json;
using TheCanonry.ApiClients.Llm;

namespace TheCanonry.Illuminator.Catalog;

/// <summary>
/// LLM-based metadata fill for image catalogs.
///
/// Ported from apps/illuminator/webui/src/lib/catalogLlmFill.ts.
///
/// Two operations:
///   FillFromTextAsync  — classify style + palette + tags from prompts/entity context
///   FillTitlesAsync    — generate evocative gallery titles
///
/// Both use a shared batch runner that processes images in chunks, calls the LLM,
/// parses the JSON array response, and applies updates to entries.
/// Returns new <see cref="ImageCatalogEntry"/> instances — entries are treated as
/// immutable and the updated list is returned (no in-place mutation of originals).
/// </summary>
public sealed class CatalogLlmFill
{
    private const int TextBatchSize = 20;
    private const int TitleBatchSize = 50;
    private const string ClassifyModel = "claude-haiku-4-5-20251001";
    private const string TitleModel = LlmModel.Opus46;

    private readonly ILlmClient _llm;

    public CatalogLlmFill(ILlmClient llm)
    {
        _llm = llm;
    }

    // =========================================================================
    // Classify Fill — assign styles, palette, tags
    // =========================================================================

    /// <summary>
    /// Batch LLM classification — text mode (from prompts and entity context).
    /// Assigns artisticStyleId, compositionStyleId, colorPaletteId, and tags
    /// for any entries that are missing those fields.
    /// Returns updated entries; unmodified entries are returned unchanged.
    /// </summary>
    public async Task<IReadOnlyList<ImageCatalogEntry>> FillFromTextAsync(
        IReadOnlyList<ImageCatalogEntry> images,
        IReadOnlyList<string> availableStyles,
        IReadOnlyList<string> availablePalettes,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        var validStyles = new HashSet<string>(availableStyles, StringComparer.Ordinal);
        var validPalettes = new HashSet<string>(availablePalettes, StringComparer.Ordinal);

        var styleBlock = BuildStyleBlock(availableStyles, availablePalettes);
        var systemPrompt = BuildClassifySystemPrompt(styleBlock);

        var candidates = images
            .Where(img => !HasFullStyleMetadata(img))
            .ToList();

        var entryMap = images.ToDictionary(img => img.ImageId, StringComparer.Ordinal);

        var batches = Batch(candidates, TextBatchSize);
        var done = 0;

        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            var entries = batch
                .Select((img, i) => $"[{i}] id=\"{img.ImageId}\"\nprompt: {Truncate(img.EntityName, 500)}")
                .ToList();

            var userPrompt = new StringBuilder();
            userPrompt.AppendLine("Classify each image. Return a JSON array where each element has \"id\", \"artisticStyleId\", \"compositionStyleId\", \"colorPaletteId\", and \"tags\".");
            userPrompt.AppendLine();
            foreach (var e in entries)
            {
                userPrompt.AppendLine(e);
                userPrompt.AppendLine();
            }

            var request = new LlmRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt.ToString().TrimEnd(),
                Model = ClassifyModel,
                MaxTokens = 4096,
                Temperature = 0.3,
                DisableStreaming = true,
            };

            try
            {
                var response = await _llm.CompleteAsync(request, ct).ConfigureAwait(false);
                if (response.Error is not null)
                {
                    done += batch.Count;
                    progress?.Report((done, candidates.Count));
                    continue;
                }

                var parsed = ExtractJsonArray(response.Text);
                foreach (var item in parsed)
                {
                    if (item.TryGetValue("id", out var idVal) && idVal is JsonElement idEl
                        && idEl.ValueKind == JsonValueKind.String)
                    {
                        var imageId = idEl.GetString()!;
                        if (!entryMap.TryGetValue(imageId, out var entry)) continue;

                        var updated = entry;

                        if (string.IsNullOrEmpty(entry.ArtisticStyleId)
                            && item.TryGetValue("artisticStyleId", out var artVal)
                            && artVal is JsonElement artEl
                            && artEl.ValueKind == JsonValueKind.String
                            && validStyles.Contains(artEl.GetString()!))
                            updated = updated with { ArtisticStyleId = artEl.GetString() };

                        if (string.IsNullOrEmpty(updated.CompositionStyleId)
                            && item.TryGetValue("compositionStyleId", out var compVal)
                            && compVal is JsonElement compEl
                            && compEl.ValueKind == JsonValueKind.String
                            && validStyles.Contains(compEl.GetString()!))
                            updated = updated with { CompositionStyleId = compEl.GetString() };

                        if (string.IsNullOrEmpty(updated.ColorPaletteId)
                            && item.TryGetValue("colorPaletteId", out var palVal)
                            && palVal is JsonElement palEl
                            && palEl.ValueKind == JsonValueKind.String
                            && validPalettes.Contains(palEl.GetString()!))
                            updated = updated with { ColorPaletteId = palEl.GetString() };

                        if (updated.Tags is not { Count: > 0 }
                            && item.TryGetValue("tags", out var tagsVal)
                            && tagsVal is JsonElement tagsEl
                            && tagsEl.ValueKind == JsonValueKind.Array)
                        {
                            var tags = tagsEl.EnumerateArray()
                                .Where(t => t.ValueKind == JsonValueKind.String)
                                .Select(t => t.GetString()!)
                                .ToList();
                            if (tags.Count > 0)
                                updated = updated with { Tags = tags };
                        }

                        if (!ReferenceEquals(updated, entry))
                            entryMap[imageId] = updated;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
            {
                // Batch failure — continue with remaining batches
            }

            done += batch.Count;
            progress?.Report((done, candidates.Count));
        }

        return images.Select(img => entryMap.TryGetValue(img.ImageId, out var updated) ? updated : img).ToList();
    }

    // =========================================================================
    // Title Fill
    // =========================================================================

    /// <summary>
    /// Generate evocative gallery titles for images that lack them.
    /// Returns updated entries; unmodified entries are returned unchanged.
    /// </summary>
    public async Task<IReadOnlyList<ImageCatalogEntry>> FillTitlesAsync(
        IReadOnlyList<ImageCatalogEntry> images,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        var candidates = images
            .Where(img => string.IsNullOrEmpty(img.Title))
            .ToList();

        var entryMap = images.ToDictionary(img => img.ImageId, StringComparer.Ordinal);

        var batches = Batch(candidates, TitleBatchSize);
        var done = 0;

        var lenses = new[]
        {
            "Lean into irony, contradiction, or unexpected juxtaposition.",
            "Lean into stillness, absence, or what's been left behind.",
            "Lean into motion, transformation, or the moment just before change.",
            "Lean into names, places, or fragments of speech that carry narrative charge.",
            "Lean into texture, material, or the physical world — how things feel, not how they look.",
            "Lean into questions, ambiguity, or double meanings.",
            "Lean into the mythic register — titles that sound like they belong in oral tradition.",
            "Lean into brevity and punch — two or three words that land hard.",
        };

        const string systemPrompt =
            "You title artwork for a fantasy world gallery. " +
            "You are given each image's entity name and style context. " +
            "Title the subtext — what the image means, not what it shows. " +
            "Each title is 1-6 words. Each title in this batch must use a different syntactic structure. " +
            "Respond with valid JSON only. No markdown, no explanation.";

        var batchIndex = 0;
        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            var lens = lenses[batchIndex % lenses.Length];
            var instruction = $"Title each image. {lens} Return a JSON array where each element has \"id\" (string) and \"title\" (string).";

            var entries = batch.Select((img, i) =>
            {
                var ctx = BuildStyleContext(img);
                return $"[{i}] id=\"{img.ImageId}\"\n{ctx}";
            }).ToList();

            var userPrompt = new StringBuilder();
            userPrompt.AppendLine(instruction);
            userPrompt.AppendLine();
            foreach (var e in entries)
            {
                userPrompt.AppendLine(e);
                userPrompt.AppendLine();
            }

            var request = new LlmRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt.ToString().TrimEnd(),
                Model = TitleModel,
                ThinkingBudget = 4096,
                MaxTokens = 4096,
                Temperature = 1.0,
                DisableStreaming = true,
            };

            try
            {
                var response = await _llm.CompleteAsync(request, ct).ConfigureAwait(false);
                if (response.Error is not null)
                {
                    done += batch.Count;
                    progress?.Report((done, candidates.Count));
                    batchIndex++;
                    continue;
                }

                var parsed = ExtractJsonArray(response.Text);
                foreach (var item in parsed)
                {
                    if (item.TryGetValue("id", out var idVal) && idVal is JsonElement idEl
                        && idEl.ValueKind == JsonValueKind.String
                        && item.TryGetValue("title", out var titleVal) && titleVal is JsonElement titleEl
                        && titleEl.ValueKind == JsonValueKind.String)
                    {
                        var imageId = idEl.GetString()!;
                        var title = titleEl.GetString()!.Trim();
                        if (!string.IsNullOrEmpty(title) && entryMap.TryGetValue(imageId, out var entry))
                            entryMap[imageId] = entry with { Title = title };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
            {
                // Batch failure — continue
            }

            done += batch.Count;
            progress?.Report((done, candidates.Count));
            batchIndex++;
        }

        return images.Select(img => entryMap.TryGetValue(img.ImageId, out var updated) ? updated : img).ToList();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static bool HasFullStyleMetadata(ImageCatalogEntry img) =>
        !string.IsNullOrEmpty(img.ArtisticStyleId)
        && !string.IsNullOrEmpty(img.CompositionStyleId)
        && !string.IsNullOrEmpty(img.ColorPaletteId)
        && img.Tags is { Count: > 0 };

    private static string BuildStyleBlock(
        IReadOnlyList<string> styles,
        IReadOnlyList<string> palettes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ARTISTIC STYLES (pick one):");
        sb.AppendLine(string.Join(", ", styles));
        sb.AppendLine();
        sb.AppendLine("COMPOSITION STYLES (pick one):");
        sb.AppendLine(string.Join(", ", styles));
        sb.AppendLine();
        sb.AppendLine("COLOR PALETTES (pick one):");
        sb.Append(string.Join(", ", palettes));
        return sb.ToString();
    }

    private static string BuildClassifySystemPrompt(string styleBlock) =>
        $"""
        You are a metadata classifier for a fantasy world image catalog.
        Given image entity names and context, classify each into the correct style categories and generate tags.

        For each image, you must select from EXACTLY these valid IDs:

        {styleBlock}

        Rules:
        - artisticStyleId: Match based on the visual technique and medium.
        - compositionStyleId: Match based on framing and subject arrangement.
        - colorPaletteId: Match based on color references, mood, and tonal qualities.
        - tags: 3-8 descriptive tags. Subject matter, mood, setting. Lowercase, no punctuation.

        You MUST use IDs from the lists above. Never invent new IDs.
        Respond with valid JSON only. No markdown, no explanation.
        """;

    private static string BuildStyleContext(ImageCatalogEntry img)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(img.ArtisticStyleId))
            parts.Add($"artistic style: {img.ArtisticStyleId}");
        if (!string.IsNullOrEmpty(img.CompositionStyleId))
            parts.Add($"composition: {img.CompositionStyleId}");
        if (!string.IsNullOrEmpty(img.ColorPaletteId))
            parts.Add($"palette: {img.ColorPaletteId}");
        if (!string.IsNullOrEmpty(img.EntityName))
            parts.Add($"entity: {img.EntityName}");
        if (!string.IsNullOrEmpty(img.EntityKind))
            parts.Add($"kind: {img.EntityKind}");
        return string.Join(", ", parts);
    }

    private static List<Dictionary<string, JsonElement>> ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end <= start) return new();

        try
        {
            var slice = text[start..(end + 1)];
            var doc = JsonDocument.Parse(slice);
            var result = new List<Dictionary<string, JsonElement>>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var dict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var prop in el.EnumerateObject())
                    dict[prop.Name] = prop.Value.Clone();
                result.Add(dict);
            }
            return result;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new();
        }
    }

    private static IEnumerable<List<T>> Batch<T>(IReadOnlyList<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
            yield return source.Skip(i).Take(size).ToList();
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen];
}
