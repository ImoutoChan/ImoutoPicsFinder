﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImoutoPicsFinder.Instagram;

public class PaidInstagramDownloader
{
    private readonly string _accessToken;

    public PaidInstagramDownloader(string accessToken) => _accessToken = accessToken;

    public async Task<Result<InstagramPost>> GetMediaOrStory(string url) 
        => url.Contains("stories") ? await GetStory(url) : await GetMedia(url);

    public async Task<Result<InstagramPost>> GetMedia(string url)
    {
        try
        {
            return await GetMediaFromInstagramInternal(url);
        }
        catch (Exception e)
        {
            return new Result<InstagramPost>(null, false, e.Message);
        }
    }
    
    public async Task<Result<InstagramPost>> GetStory(string url)
    {
        try
        {
            return await GetStoryFromInstagramInternal(url);
        }
        catch (Exception e)
        {
            return new Result<InstagramPost>(null, false, e.Message);
        }
    }

    private async Task<Result<InstagramPost>> GetStoryFromInstagramInternal(string url)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-access-key", _accessToken);
        client.DefaultRequestHeaders.Add("accept", "application/json");
        
        var infoUrl = $"https://api.hikerapi.com/v1/story/by/url?url={WebUtility.UrlEncode(url)}";
        var postResponseString = await client.GetStringAsync(infoUrl);
        var instagramPost = JsonSerializer.Deserialize<PaidInstagramStory>(postResponseString);

        var isVideo = instagramPost.VideoUrl != null;

        if (isVideo)
        {
            var videoBytes = await client.GetByteArrayAsync(instagramPost.VideoUrl);
            return new Result<InstagramPost>(
                new InstagramPost(
                    url,
                    new List<InstagramMedia>
                        { new(
                            videoBytes, 
                            GetFileName(instagramPost.VideoUrl), 
                            InstagramMediaType.Video) },
                    ""),
                true, null);
        }
        else 
        {
            var photoBytes = await client.GetByteArrayAsync(instagramPost.ThumbnailUrl);
            return new Result<InstagramPost>(
                new InstagramPost(
                    url,
                    new List<InstagramMedia>
                        { new(
                            photoBytes, 
                            GetFileName(instagramPost.ThumbnailUrl), 
                            InstagramMediaType.Photo) },
                    ""),
                true, null);
        }
    }

    private async Task<Result<InstagramPost>> GetMediaFromInstagramInternal(string url)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-access-key", _accessToken);
        client.DefaultRequestHeaders.Add("accept", "application/json");
        
        var postInfoUrl = $"https://api.hikerapi.com/v2/media/by/url?url={WebUtility.UrlEncode(url)}";
        var postResponseString = await client.GetStringAsync(postInfoUrl);
        var instagramPost = JsonSerializer.Deserialize<PaidInstagramPost>(postResponseString);
        
        if (!instagramPost.Items.Any())
            return new Result<InstagramPost>(null, false, "No items found by provided url");

        var item = instagramPost.Items.First();

        var caption = item.Caption?.Text;
        var type = item.ProductType switch
        {
            "carousel_container" => ProductType.CarouselContainer,
            "clips" => ProductType.Clips,
            "feed" => ProductType.Feed,
            _ => throw new ArgumentOutOfRangeException(item.ProductType)
        };

        if (type == ProductType.CarouselContainer)
        {
            var files = new List<InstagramMedia>();
            foreach (var media in item.CarouselMedia)
            {
                if (media.MediaType == 1)
                {
                    var maxImage = media.ImageVersions2.Candidates.MaxBy(x => x.Width);
                    var photoBytes = await client.GetByteArrayAsync(maxImage.Url);
                    files.Add(new (photoBytes, GetFileName(maxImage.Url), InstagramMediaType.Photo));
                }
                else
                {
                    throw new NotImplementedException($"Unknown media type in carousel {media.MediaType}");
                }
            }
            
            return new Result<InstagramPost>(new InstagramPost(url, files, caption), true, null);
        }
        else if (type == ProductType.Clips)
        {
            var maxVideo = item.VideoVersions.MaxBy(x => x.Width);
            var videoBytes = await client.GetByteArrayAsync(maxVideo.Url);
            return new Result<InstagramPost>(
                new InstagramPost(
                    url,
                    new List<InstagramMedia> { new (videoBytes, GetFileName(maxVideo.Url), InstagramMediaType.Video) }, 
                    caption),
                true, null);
        }
        else if (type == ProductType.Feed)
        {
            var maxFeedImage = item.ImageVersions2.Candidates.MaxBy(x => x.Width);
            var feedImageBytes = await client.GetByteArrayAsync(maxFeedImage.Url);
            return new Result<InstagramPost>(
                new InstagramPost(
                    url,
                    new List<InstagramMedia>
                        { new(feedImageBytes, GetFileName(maxFeedImage.Url), InstagramMediaType.Photo) },
                    caption),
                true, null);
        }
        
        return new Result<InstagramPost>(null, false, "Unknown product type");
    }
    
    private string GetFileName(string url) => Path.GetFileName(url).Split('?').First();
}

public record Result<T>(T Value, bool IsSuccess, string Error);

public record InstagramPost(string Url, IReadOnlyCollection<InstagramMedia> Files, string Caption);

public record InstagramMedia(byte[] File, string Name, InstagramMediaType Type);

public enum InstagramMediaType
{
    Video,
    Photo
}

public enum ProductType 
{
    CarouselContainer,
    Clips,
    Feed
}