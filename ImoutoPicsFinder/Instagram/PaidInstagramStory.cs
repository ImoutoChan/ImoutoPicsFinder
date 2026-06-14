using System.Text.Json.Serialization;

namespace ImoutoPicsFinder.Instagram;

public record PaidInstagramStory(
    [property: JsonPropertyName("product_type")] string ProductType,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("video_url")] string VideoUrl,
    [property: JsonPropertyName("user")] User User,
    [property: JsonPropertyName("taken_at")] object TakenAt,
    [property: JsonPropertyName("original_width")] int? OriginalWidth = null,
    [property: JsonPropertyName("original_height")] int? OriginalHeight = null
);

public record User(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("full_name")] string FullName
);
