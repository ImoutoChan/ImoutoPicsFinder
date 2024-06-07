using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ImoutoPicsFinder.TikTok;

public record JsonModel(
    [property: JsonPropertyName("aweme_list")] IReadOnlyList<AwemeListItem> AwemeList
);

public record Author(
    [property: JsonPropertyName("nickname")] string Nickname,
    [property: JsonPropertyName("unique_id")] string UniqueId
);

public record AwemeListItem(
    [property: JsonPropertyName("aweme_id")] string AwemeId,
    [property: JsonPropertyName("author")] Author Author,
    [property: JsonPropertyName("video")] Video Video,
    [property: JsonPropertyName("image_post_info")] ImagePostInfo ImagePostInfo
);

public record BitRate(
    [property: JsonPropertyName("gear_name")] string GearName,
    [property: JsonPropertyName("quality_type")] int? QualityType,
    [property: JsonPropertyName("bit_rate")] int? BitRateProp,
    [property: JsonPropertyName("play_addr")] PlayAddr PlayAddr,
    [property: JsonPropertyName("is_bytevc1")] int? IsBytevc1,
    [property: JsonPropertyName("dub_infos")] object DubInfos,
    [property: JsonPropertyName("HDR_type")] string HDRType,
    [property: JsonPropertyName("HDR_bit")] string HDRBit
);

public record DisplayImage(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("url_list")] IReadOnlyList<string> UrlList,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("url_prefix")] object UrlPrefix
);

public record DownloadAddr(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("url_list")] IReadOnlyList<string> UrlList,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("data_size")] int? DataSize,
    [property: JsonPropertyName("url_prefix")] object UrlPrefix
);

public record Image(
    [property: JsonPropertyName("display_image")] DisplayImage DisplayImage,
    [property: JsonPropertyName("owner_watermark_image")] OwnerWatermarkImage OwnerWatermarkImage,
    [property: JsonPropertyName("user_watermark_image")] UserWatermarkImage UserWatermarkImage,
    [property: JsonPropertyName("thumbnail")] Thumbnail Thumbnail,
    [property: JsonPropertyName("bitrate_images")] object BitrateImages
);

public record ImagePostCover(
    [property: JsonPropertyName("display_image")] DisplayImage DisplayImage,
    [property: JsonPropertyName("owner_watermark_image")] OwnerWatermarkImage OwnerWatermarkImage,
    [property: JsonPropertyName("user_watermark_image")] UserWatermarkImage UserWatermarkImage,
    [property: JsonPropertyName("thumbnail")] Thumbnail Thumbnail,
    [property: JsonPropertyName("bitrate_images")] object BitrateImages
);

public record ImagePostInfo(
    [property: JsonPropertyName("images")] IReadOnlyList<Image> Images,
    [property: JsonPropertyName("image_post_cover")] ImagePostCover ImagePostCover,
    [property: JsonPropertyName("post_extra")] string PostExtra
);

public record OwnerWatermarkImage(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("url_list")] IReadOnlyList<string> UrlList,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("url_prefix")] object UrlPrefix
);

public record PlayAddr(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("url_list")] IReadOnlyList<string> UrlList,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("url_key")] string UrlKey,
    [property: JsonPropertyName("data_size")] int? DataSize,
    [property: JsonPropertyName("file_hash")] string FileHash,
    [property: JsonPropertyName("url_prefix")] object UrlPrefix
);

public record Thumbnail(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("url_list")] IReadOnlyList<string> UrlList,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("url_prefix")] object UrlPrefix
);
public record UserWatermarkImage(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("url_list")] IReadOnlyList<string> UrlList,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("url_prefix")] object UrlPrefix
);

public record Video(
    [property: JsonPropertyName("play_addr")] PlayAddr PlayAddr,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("download_addr")] DownloadAddr DownloadAddr,
    [property: JsonPropertyName("has_watermark")] bool? HasWatermark,
    [property: JsonPropertyName("bit_rate")] IReadOnlyList<BitRate> BitRate,
    [property: JsonPropertyName("duration")] int? Duration,
    [property: JsonPropertyName("cdn_url_expired")] int? CdnUrlExpired
);
