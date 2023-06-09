﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ImoutoPicsFinder.TikTok;

public class TikTokDownloader
{
    public async Task<DownloadData?> GetContentFromTikTokAsync(string url)
    {
        // awemeId
        string? tiktokId = null;
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
            string? json = null;
            try
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://api16-normal-c-useast1a.tiktokv.com/aweme/v1/feed/?aweme_id={tiktokId}"),
                    Method = HttpMethod.Get
                };
                request.Headers.Add("Accept", "application/json");
                var response = await new HttpClient().SendAsync(request);
                json = await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                // ignored
            }

            if (json == null) 
                return null;

            JObject obj;
            try
            {
                obj = JsonConvert.DeserializeObject<JObject>(json)!;
            }
            catch (Exception)
            {
                return null;
            }

            var awemeList = obj["aweme_list"] as JArray;
            var targetAweme = awemeList![0];
            var urlVideoList = new List<UrlDescription>();
            var video = targetAweme["video"];
            if (video != null)
            {
                var bitRate = video["bit_rate"] as JArray;
                foreach (var urlVideo in bitRate!)
                {
                    var gearName = urlVideo["gear_name"]!.ToString();
                    var playAddr = urlVideo["play_addr"]!["url_list"]![0]!.ToString();
                    urlVideoList.Add(new UrlDescription
                    {
                        Url = playAddr,
                        FileName = $"no_watermark_{gearName}_{tiktokId}.mp4"
                    });
                    break;
                }
            }

            return new DownloadData
            {
                VideoList = urlVideoList
            };
        }

        return null;
    }

    private static readonly Regex TikTokIdRegex = new(@".*\/(\d*).*", RegexOptions.Compiled);
    
    public class UrlDescription
    {
        public string Url { get; set; } = default!;
        public string FileName { get; set; } = default!;
    }

    public class DownloadData
    {
        public List<UrlDescription> VideoList { get; set; } = default!;
    }
}
