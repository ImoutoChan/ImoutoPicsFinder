using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ImoutoPicsFinder.Instagram;

public record PaidInstagramPost(
    [property: JsonPropertyName("items")] IReadOnlyList<Item> Items
);

public record Item(
    [property: JsonPropertyName("caption")] Caption Caption,
    [property: JsonPropertyName("product_type")] string ProductType,
    [property: JsonPropertyName("carousel_media")] IReadOnlyList<CarouselMedium> CarouselMedia,
    [property: JsonPropertyName("video_versions")] IReadOnlyList<VideoVersion> VideoVersions,
    [property: JsonPropertyName("image_versions2")] ImageVersions2 ImageVersions2,
    [property: JsonPropertyName("user")] User User,
    [property: JsonPropertyName("taken_at")] object TakenAt

    
);

public record Caption(
    [property: JsonPropertyName("text")] string Text
);

public record CarouselMedium(
    // 1 -- image
    [property: JsonPropertyName("media_type")] int? MediaType,
    [property: JsonPropertyName("image_versions2")] ImageVersions2 ImageVersions2,
    [property: JsonPropertyName("video_versions")] IReadOnlyList<VideoVersion> VideoVersions,
    [property: JsonPropertyName("original_width")] int? OriginalWidth,
    [property: JsonPropertyName("original_height")] int? OriginalHeight
);

public record ImageVersions2(
    [property: JsonPropertyName("candidates")] IReadOnlyList<Candidate> Candidates
);

public record Candidate(
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("url")] string Url
);

public record VideoVersion(
    [property: JsonPropertyName("type")] int? Type,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] string Id
);




