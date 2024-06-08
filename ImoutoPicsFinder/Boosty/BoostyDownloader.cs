#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ImoutoPicsFinder.Boosty.DialogJsonModel;
using Newtonsoft.Json;

namespace ImoutoPicsFinder.Boosty;

public class BoostyDownloader
{
    public async Task<byte[]> DownloadLinkAsync(string url)
    {
        var httpClient = GetHttpClient();
        return await httpClient.GetByteArrayAsync(url);
    }

    public async IAsyncEnumerable<BoostyUrlInfo> GetLinksAsync(string url)
    {
        var httpClient = GetHttpClient();
        var info = ParseBoostyUrlAsync(url);
        
        if (info is MessageBoostyUrlInfo messageInfo)
        {
            await foreach (var p in GetLinksFromMessage(httpClient, messageInfo)) 
                yield return p;
        }
        else if (info is PostBoostyUrlInfo postInfo)
        {
            await foreach (var p in GetLinksFromPost(httpClient, postInfo)) 
                yield return p;
        }
    }

    private static async IAsyncEnumerable<BoostyUrlInfo> GetLinksFromMessage(
        HttpClient httpClient, 
        MessageBoostyUrlInfo messageInfo)
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + messageInfo.AuthToken);
            
        // GET https://api.boosty.to/v1/dialog/{dialogId}
        var response = await httpClient.GetStringAsync($"https://api.boosty.to/v1/dialog/{messageInfo.DialogId}");
        var model = JsonConvert.DeserializeObject<DialogJsonModel.DialogJsonModel>(response) 
                    ?? throw new InvalidOperationException("Failed to deserialize response");

        var selectedMessage = model.Messages.MessageList.FirstOrDefault(x => x.MessageId == messageInfo.MessageId);
        var min = model.Messages.MessageList.Select(x => x.MessageId).Min();
        var author = model.Author;

        while (selectedMessage == null 
               && model.Messages.MessageList.Select(x => x.MessageId).All(x => x > messageInfo.MessageId))
        {
            // GET https://api.boosty.to/v1/dialog/{dialogId}/message/?limit=20&reverse=true&offset={min}
            response = await httpClient.GetStringAsync(
                $"https://api.boosty.to/v1/dialog/{messageInfo.DialogId}/message/?limit=20&reverse=true&offset={min}");
            
            var nextModel = JsonConvert.DeserializeObject<Messages>(response) 
                            ?? throw new InvalidOperationException("Failed to deserialize response");

            selectedMessage = nextModel.MessageList.FirstOrDefault(x => x.MessageId == messageInfo.MessageId);
            min = nextModel.MessageList.Select(x => x.MessageId).Min();
        }

        if (selectedMessage == null)
            throw new InvalidOperationException("Message not found");
            
        foreach (var media in selectedMessage.Medium)
        {
            if (media.Type == "image")
            {
                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Photo, media.Url!, fileName);
            }
            else if (media.Type == "ok_video")
            {
                var videoUrl = media.PlayerUrls!
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                    .OrderBy(x => GetQualityPriority(x.Type))
                    .Select(x => x.Url)
                    .First();

                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Video, videoUrl, fileName);
            }
        }

        foreach (var media in selectedMessage.TeaserMedium)
        {
            if (media.Type == "image")
            {
                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Photo, media.Url!, fileName);
            }
            else if (media.Type == "ok_video")
            {
                var videoUrl = media.PlayerUrls!
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                    .OrderBy(x => GetQualityPriority(x.Type))
                    .Select(x => x.Url)
                    .First();

                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Video, videoUrl, fileName);
            }
        }
    }

    private static HttpClient GetHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
        return httpClient;
    }

    private static string GetFileName(string name, string url, string mediaId)
    {
        var fileName = $"{name} ({url}) " + mediaId + ".jpg";
        return fileName;
    }

    private static async IAsyncEnumerable<BoostyUrlInfo> GetLinksFromPost(
        HttpClient httpClient, 
        PostBoostyUrlInfo messageInfo)
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + messageInfo.AuthToken);
            
        // GET https://api.boosty.to/v1/blog/{username}/post/{postId}
        var response = await httpClient.GetStringAsync($"https://api.boosty.to/v1/blog/{messageInfo.Username}/post/{messageInfo.PostId}");
        var model = JsonConvert.DeserializeObject<PostJsonModel.PostJsonModel>(response) 
                    ?? throw new InvalidOperationException("Failed to deserialize response");

        var author = model.Author;
            
        foreach (var media in model.Medium)
        {
            if (media.Type == "image")
            {
                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Photo, media.Url!, fileName);
            }
            else if (media.Type == "ok_video")
            {
                var videoUrl = media.PlayerUrls!
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                    .OrderBy(x => GetQualityPriority(x.Type))
                    .Select(x => x.Url)
                    .First();

                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Video, videoUrl, fileName);
            }
        }

        foreach (var media in model.TeaserMedium)
        {
            if (media.Type == "image")
            {
                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Photo, media.Url!, fileName);
            }
            else if (media.Type == "ok_video")
            {
                var videoUrl = media.PlayerUrls!
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                    .OrderBy(x => GetQualityPriority(x.Type))
                    .Select(x => x.Url)
                    .First();

                var fileName = GetFileName(author.Name, author.Url, media.MediaId);
                yield return new BoostyUrlInfo(BoostyMediaType.Video, videoUrl, fileName);
            }
        }
    }

    /// <summary>
    /// The lower - the better.
    /// </summary>
    private static int GetQualityPriority(string qualityType) 
        => qualityType switch
        {
            "ultra_hd" => 0,
            "quad_hd" => 1,
            "full_hd" => 2,
            "high" => 3,
            "medium" => 4,
            "low" => 5,
            "tiny" => 6,
            "lowest" => 7,
            "hls" => 8,
            "dash" => 9,
            "dash_uni" => 10,
            "live_ondemand_hls" => 11,
            "live_cmaf" => 12,
            "live_dash" => 13,
            "live_hls" => 14,
            "live_playback_dash" => 15,
            "live_playback_hls" => 16,
            _ => 17
        };

    private static IBoostyUrlInfo ParseBoostyUrlAsync(string url)
    {
        var uri1 = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri1.Query);        
        var segments = uri1.Segments.Select(x => x.Trim('/')).ToArray();

        
        var isMessage = segments.Contains("messages");

        if (isMessage)
        {
            var mediaId = segments[^1];
            var messageId = int.Parse(segments[^2]);
            var dialogId = query["dialogId"] ?? throw new InvalidOperationException("Dialog id is required");
            var authToken = query["auth"] ?? throw new InvalidOperationException("Auth token is required");
            
            return new MessageBoostyUrlInfo(
                AuthToken: authToken,
                DialogId: dialogId,
                MessageId: messageId,
                MediaId: mediaId);
        }
        
        var isPost = segments.Contains("posts");

        if (isPost)
        {
            var postsIndex = Array.IndexOf(segments, "posts");
            var postId = segments[postsIndex + 1];
            var username = segments[postsIndex - 1];

            var mediaIndex = Array.IndexOf(segments, "media");
            
            var mediaId = mediaIndex != -1 ? segments[mediaIndex+ 1] : null;
            
            var authToken = query["auth"] ?? throw new InvalidOperationException("Auth token is required");
            
            return new PostBoostyUrlInfo(
                AuthToken: authToken,
                PostId: postId,
                MediaId: mediaId,
                Username: username);
        }

        throw new InvalidOperationException("Unknown boosty url type");
    }
}

internal record MessageBoostyUrlInfo(
    string AuthToken,
    string DialogId,
    int MessageId,
    string MediaId) : IBoostyUrlInfo;

internal record PostBoostyUrlInfo(
    string AuthToken,
    string PostId,
    string? MediaId,
    string Username) : IBoostyUrlInfo;

internal interface IBoostyUrlInfo;

public record BoostyUrlInfo(BoostyMediaType Type, string Url, string FileName);

public enum BoostyMediaType
{
    Photo,
    Video
}
