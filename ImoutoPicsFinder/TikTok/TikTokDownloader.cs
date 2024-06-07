#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImoutoPicsFinder.TikTok;

public class TikTokDownloader
{
    public async Task<DownloadData?> GetContentFromTikTokIdAsync(string? id)
    {        
        if (id != null)
        {
            return await GetFromTikTokIdAsync(id);
        }

        return null;
    }

    public async Task<DownloadData?> GetContentFromTikTokAsync(string url)
    {
        // awemeId
        string? tiktokId = null;

        if (url.Contains("video/"))
        {
            var match = VideoTikTokIdRegex.Match(url);
            if (match.Groups.Count > 1) 
                tiktokId = match.Groups[1].Value;
        }

        if (tiktokId == null)
        {
            try
            {
                var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
                var response = await client.GetAsync(url);
                var realUrl = response.Headers.GetValues("Location").FirstOrDefault();
                var match = TikTokIdRegex.Match(realUrl ?? "");
                if (match.Groups.Count > 1)
                    tiktokId = match.Groups[1].Value;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        if (tiktokId == null)
        {
            try
            {
                var response = await new HttpClient().GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var splitContent = content.Split(new[] { "\"aweme_id\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                if (splitContent.Length > 1)
                    tiktokId = splitContent[1].Split(new[] { "\"" }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            catch (Exception)
            {
                // ignored
            }
        }

        if (tiktokId != null)
        {
            return await GetFromTikTokIdAsync(tiktokId);
        }

        return null;
    }

    private static async Task<DownloadData?> GetFromTikTokIdAsync(string tiktokId)
    {
        string? json = null;
        try
        {
            var request = new HttpRequestMessage
            {
                //RequestUri = new Uri($"https://api16-normal-c-useast1a.tiktokv.com/aweme/v1/feed/?aweme_id={tiktokId}"),
                //RequestUri = new Uri($"https://api22-normal-c-alisg.tiktokv.com/aweme/v1/feed/?iid=7318518857994389254&device_id=7318517321748022790&channel=googleplay&app_name=musical_ly&version_code=300904&device_platform=android&device_type=ASUS_Z01QD&os_version=9&aweme_id={tiktokId}"),
                RequestUri = new Uri($"https://api22-normal-c-alisg.tiktokv.com/aweme/v1/feed/?aweme_id={tiktokId}&iid=7318518857994389254&device_id=7318517321748022790&channel=googleplay&app_name=musical_ly&version_code=300904&device_platform=android&device_type=ASUS_Z01QD&version=9"),
                Method = HttpMethod.Options
            };
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.3");
            var response = await new HttpClient().SendAsync(request);
            json = await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            // ignored
        }

        if (string.IsNullOrWhiteSpace(json))
            return null;

        JsonModel jsonResponse;
        try
        {
            jsonResponse = JsonSerializer.Deserialize<JsonModel>(json)!;
        }
        catch (Exception)
        {
            return null;
        }

        var targetAweme = jsonResponse.AwemeList[0];

        if (targetAweme.AwemeId != tiktokId)
        {
            // video was deleted
            return null;
        }
        
        var namePrefix = targetAweme.Author.UniqueId + " (" + targetAweme.Author.Nickname + ")";
        
        var urlVideoList = new List<UrlDescription>();
        var video = targetAweme.Video;
        if (video != null)
        {
            urlVideoList.Add(new UrlDescription
            {
                Url = video.PlayAddr.UrlList[0],
                FileName = $"{namePrefix}_play_{tiktokId}.mp4"
            });
            
            foreach (var urlVideo in video.BitRate)
            {
                var gearName = urlVideo.GearName;
                var playAddr = urlVideo.PlayAddr.UrlList[0];
                urlVideoList.Add(new UrlDescription
                {
                    Url = playAddr,
                    FileName = $"{namePrefix}_{gearName}_{tiktokId}.mp4"
                });
                break;
            }
        }

        var imageList = new List<UrlDescription>();
        var imageInfo = targetAweme.ImagePostInfo;
        if (imageInfo != null)
        {
            var counter = 0;
            foreach (var image in imageInfo.Images)
            {
                imageList.Add(
                    new UrlDescription
                    {
                        // url_list[0] contains a webp
                        // url_list[1] contains a jpeg
                        Url = image.DisplayImage.UrlList[1],
                        FileName = $"{namePrefix}_{tiktokId}_{counter++}.jpg"
                    });
            }
        }

        return new DownloadData
        {
            VideoList = urlVideoList,
            ImageList = imageList
        };
    }

    private static readonly Regex TikTokIdRegex = new(@".*\/(\d*).*", RegexOptions.Compiled);
    private static readonly Regex VideoTikTokIdRegex = new(@"video\/(\d*).*", RegexOptions.Compiled);
    
    public class UrlDescription
    {
        public string Url { get; set; } = default!;
        public string FileName { get; set; } = default!;
    }

    public class DownloadData
    {
        public List<UrlDescription> VideoList { get; set; } = default!;
        public List<UrlDescription> ImageList { get; set; } = default!;
    }
}
