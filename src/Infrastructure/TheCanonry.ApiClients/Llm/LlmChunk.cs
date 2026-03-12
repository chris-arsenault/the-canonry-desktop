namespace TheCanonry.ApiClients.Llm;

/// <summary>
/// The type of content carried by a streaming chunk.
/// </summary>
public enum LlmChunkType
{
    Text,
    Thinking,
    Done,
    Error,
}

/// <summary>
/// A single chunk emitted during streaming completion.
/// </summary>
public sealed record LlmChunk(LlmChunkType Type, string Content);
