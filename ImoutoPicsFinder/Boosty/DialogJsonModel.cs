#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace ImoutoPicsFinder.Boosty.DialogJsonModel;

public record DialogJsonModel(
    [property: JsonPropertyName("messages")] Messages Messages,
    [property: JsonPropertyName("chatmate")] Chatmate Author
);

public record Chatmate(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url
);

public record Messages(
    [property: JsonPropertyName("data")] IReadOnlyList<Message> MessageList
);

public record Message(
    [property: JsonPropertyName("id")] int MessageId,
    [property: JsonPropertyName("data")] IReadOnlyList<Medium> Medium,
    [property: JsonPropertyName("teaser")] IReadOnlyList<Medium> TeaserMedium
);

public record Medium(
    [property: JsonPropertyName("id")] string MediaId,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("playerUrls")] IReadOnlyList<PlayerUrl>? PlayerUrls
);

public record PlayerUrl(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("url")] string Url
);
