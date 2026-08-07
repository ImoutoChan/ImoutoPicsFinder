#nullable enable
using System.Text.Json.Serialization;

namespace ImoutoPicsFinder.Danbooru;

public record DanbooruIqdbMatch(
    [property: JsonPropertyName("post_id")] int PostId,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("post")] DanbooruIqdbPost? Post
);

public record DanbooruIqdbPost(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("image_width")] int Width,
    [property: JsonPropertyName("image_height")] int Height
);
