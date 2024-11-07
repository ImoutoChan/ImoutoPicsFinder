#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace ImoutoPicsFinder.Boosty.PostJsonModel;

public record PostJsonModel(
    [property: JsonPropertyName("id")] string PostId,
    [property: JsonPropertyName("data")] IReadOnlyList<Medium> Medium,
    [property: JsonPropertyName("teaser")] IReadOnlyList<Medium> TeaserMedium,
    [property: JsonPropertyName("user")] User Author
);

public record User(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("blogUrl")] string Url
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
