using System.Text.Json.Serialization;

namespace ImoutoPicsFinder.Instagram;

public record PaidInstagramStory(
    [property: JsonPropertyName("product_type")] string ProductType,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("video_url")] string VideoUrl
);
