#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace ImoutoPicsFinder.Boosty.DialogJsonModel;

public record DialogJsonModel(
    [property: JsonProperty("messages")] Messages Messages,
    [property: JsonProperty("chatmate")] Chatmate Author
);

public record Chatmate(
    [property: JsonProperty("id")] int? Id,
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("url")] string Url
);

public record Messages(
    [property: JsonProperty("data")] IReadOnlyList<Message> MessageList
);

public record Message(
    [property: JsonProperty("id")] int MessageId,
    [property: JsonProperty("data")] IReadOnlyList<Medium> Medium,
    [property: JsonProperty("teaser")] IReadOnlyList<Medium> TeaserMedium
);

public record Medium(
    [property: JsonProperty("id")] string MediaId,
    [property: JsonProperty("type")] string Type,
    [property: JsonProperty("url")] string? Url,
    [property: JsonProperty("playerUrls")] IReadOnlyList<PlayerUrl>? PlayerUrls
);

public record PlayerUrl(
    [property: JsonProperty("type")] string Type,
    [property: JsonProperty("url")] string Url
);
