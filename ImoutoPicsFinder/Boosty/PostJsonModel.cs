#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace ImoutoPicsFinder.Boosty.PostJsonModel;

public record PostJsonModel(
    [property: JsonProperty("id")] string PostId,
    [property: JsonProperty("data")] IReadOnlyList<Medium> Medium,
    [property: JsonProperty("teaser")] IReadOnlyList<Medium> TeaserMedium,
    [property: JsonProperty("user")] User Author
);

public record User(
    [property: JsonProperty("id")] int? Id,
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("blogUrl")] string Url
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
