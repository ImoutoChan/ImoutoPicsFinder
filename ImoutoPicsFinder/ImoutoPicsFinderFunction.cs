﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ImoutoPicsFinder.Boosty;
using ImoutoPicsFinder.FileSplitter;
using ImoutoPicsFinder.Instagram;
using ImoutoPicsFinder.TikTok;
using IqdbApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ImoutoPicsFinder;

public class ImoutoPicsFinderFunction
{
    private readonly ILogger<ImoutoPicsFinderFunction> _logger;

    public ImoutoPicsFinderFunction(ILogger<ImoutoPicsFinderFunction> logger) => _logger = logger;

    [Function("ImoutoPicsBoostyFinder")]
    public async Task ProcessBoosty(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest request)
    {
        try
        {
            _logger.LogInformation("PicsFinder boosty function triggered");
            _logger.LogInformation(request.GetDisplayUrl());
        
            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var update = JsonSerializer.Deserialize<Update>(requestBody, JsonBotAPI.Options);
        
            var client = GetTelegramBotClient(_logger);
            await DownloadBoostyAsync(client, update.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to process boosty request: " + e.Message);
            throw;
        }
    }
    
    
    [Function("ImoutoPicsFinder")]
    public async Task Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] 
        HttpRequest request)
    {
        try
        {
            await ProcessTelegramUpdate(request); 
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to process telegram request: " + e.Message);
        }
    }

    private async Task ProcessTelegramUpdate(HttpRequest request)
    {
        _logger.LogInformation("PicsFinder function triggered");
        _logger.LogInformation(request.GetDisplayUrl());

        var client = GetTelegramBotClient(_logger);

        var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        _logger.LogInformation(requestBody);
        
        var update = JsonSerializer.Deserialize<Update>(requestBody, JsonBotAPI.Options);

        var task = ProcessUpdateAsync(update, client, requestBody);
        
        var timeout = Task.Delay(35 * 1000);
        if (await Task.WhenAny(task, timeout) == timeout)
        {
            if (update.Message != null)
            {
                try
                {
                    await client.SendMessage(
                        update.Message.Chat.Id,
                        "Took too much time, sorry, wait a little bit more! uwu! 🍑");
                }
                catch
                {
                    // ignore
                }
            }
        }
        else
        {
            await task;
        }  
    }

    private async Task ProcessUpdateAsync(
        Update update,
        TelegramBotClient client,
        string requestBody)
    {
        if (update.Message?.Text == "/start")
        {
            await client.SendMessage(
                update.Message.Chat.Id,
                "Send me a picture and I will try to find the source! uwu! 🍑",
                replyParameters: update.Message);
            return;
        }
        
        if (update.Message?.Text?.Contains("tiktok.com") == true)
        {
            await DownloadTikTokAsync(client, update.Message);
            return;
        }
        
        if (update.Message?.Text?.Contains("instagram.com") == true)
        {
            await DownloadInstagramAsync(client, update.Message);
            return;
        }
        
        if (update.Message?.Text?.StartsWith("/tiktok") == true)
        {
            await DownloadTikTokAsync(client, update.Message, update.Message?.Text.Split(" ").LastOrDefault());
            return;
        }
        
        if (update.Message?.Text?.Contains("boosty.to") == true)
        {
            var functionClient = new HttpClient();
            functionClient.Timeout = TimeSpan.FromMinutes(30);
            
            var postTask = functionClient
                .PostAsync("https://imoutopicsfinder.azurewebsites.net/api/ImoutoPicsBoostyFinder",
                    new StringContent(requestBody));

            var timer = Task.Delay(30 * 1000);
            
            var task = await Task.WhenAny(postTask, timer);

            if (task == timer)
            {
                _logger.LogInformation("Boosty function will be processed outside of the current scope");
            }
            else
            {
                await task;
            }

            return;
        }
        
        if (update.Message == null
            ||update.Type != UpdateType.Message
            || update.Message?.Photo == null
            || update.Message?.Photo.Length == 0)
        {
            return;
        }

        try
        {
            var iqdb = new IqdbClient();
            iqdb.ConfigureHttpClient(x => x.Timeout = TimeSpan.FromSeconds(1000));
            var picsFinder = new PicsFinder(client, iqdb);
            await picsFinder.ProcessPhotoPostAsync(update.Message!);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PicsFinder function failed" + e.Message);
            await client.SendMessage(
                update.Message!.Chat.Id,
                "Oops! Can't process your picture, please try again uwu 🍑" + "\n" + e.Message,
                replyParameters: update.Message);
        }
    }

    private static async Task DownloadInstagramAsync(
        ITelegramBotClient client,
        Message message)
    {
        try
        {
            var instagramAccessToken = Environment.GetEnvironmentVariable("InstagramAccessToken") 
                                       ?? throw new Exception("Unable to read InstagramAccessToken");
            
            var downloader = new PaidInstagramDownloader(instagramAccessToken);
            var url = message.Text?.Split(" ").First(x => x.Contains("instagram.com")) ?? "";
            
            var result = await downloader.GetMediaOrStory(url);

            if (!result.IsSuccess)
            {
                await client.SendMessage(
                    message.Chat.Id,
                    "Oops! Can't process your insta post, please try again uwu 🍑" + "\n" + result.Error,
                    replyParameters: message);
                return;
            }

            var content = result.Value.Files;

            if (!content.Any())
            {
                await client.SendMessage(
                    message.Chat.Id,
                    "Oops! Can't process your insta post, please try again uwu 🍑",
                    replyParameters: message);
                return;
            }

            foreach (var mediaPhotoChunk in content.Chunk(10))
            {
                if (mediaPhotoChunk.Length > 1)
                {
                    await client.SendMediaGroup(
                        message.Chat.Id,
                        mediaPhotoChunk.Select(
                            x => x.Type switch
                            {
                                InstagramMediaType.Video => new InputMediaVideo(
                                    new InputFileStream(new MemoryStream(x.File), x.Name)) as IAlbumInputMedia,
                                InstagramMediaType.Photo => new InputMediaPhoto(
                                    new InputFileStream(new MemoryStream(x.File), x.Name)),
                                _ => throw new ArgumentOutOfRangeException()
                            }).ToList(),
                        replyParameters: message);
                }
                else
                {
                    var item = mediaPhotoChunk.First();
                    if (item.Type == InstagramMediaType.Photo)
                    {
                        await client.SendPhoto(
                            message.Chat.Id,
                            new InputFileStream(new MemoryStream(item.File), item.Name),
                            replyParameters: message);
                    }
                    else
                    {
                        await client.SendVideo(
                            message.Chat.Id,
                            new InputFileStream(new MemoryStream(item.File), item.Name),
                            replyParameters: message);
                    }
                }
            }

            foreach (var item in content.Where(x => x.Type == InstagramMediaType.Photo))
            {
                await client.SendDocument(
                    message.Chat.Id,
                    new InputFileStream(new MemoryStream(item.File), item.Name),
                    replyParameters: message);
            }
        }
        catch (Exception e)
        {
            await client.SendMessage(
                message.Chat.Id,
                "Oops! Can't process your insta post, please try again uwu 🍑" + "\n" + e.Message,
                replyParameters: message);
        }
    }
    
    private static async Task DownloadBoostyAsync(ITelegramBotClient client, Message message)
    {
        var currentUrl = string.Empty;
        try
        {
            var url = message.Text;
            var downloader = new BoostyDownloader();
            
            var links = await downloader.GetLinksAsync(url!).ToListAsync();

            if (!links.Any())
            {
                await client.SendMessage(
                    message.Chat.Id,
                    "Oops! Your boosty link is kinda empty and lonely, please try again uwu 🍑",
                    replyParameters: message);
                return;
            }

            var videos = links.Where(x => x.Type == BoostyMediaType.Video).ToList();
            var images = links.Where(x => x.Type == BoostyMediaType.Photo).ToList();
            
            foreach (var video in videos)
            {
                currentUrl = video.Url;
                var bytes = await downloader.DownloadLinkAsync(video.Url);

                if (FileSplitterToZip.TrySplitFile(bytes, video.FileName, out var split))
                {
                    foreach (var (content, fileName) in split)
                    {
                        await client.SendDocument(
                            message.Chat.Id,
                            new InputFileStream(new MemoryStream(content), fileName),
                            replyParameters: message);
                    }
                }
                else
                {
                    await client.SendVideo(
                        message.Chat.Id,
                        new InputFileStream(new MemoryStream(bytes), video.FileName),
                        replyParameters: message);
                }
            }

            if (images.Any())
            {
                var downloadedImages = new List<(byte[] Content, string FileName)>();
                foreach (var image in images)
                {
                    currentUrl = image.Url;
                    var bytes = await downloader.DownloadLinkAsync(image.Url);
                    downloadedImages.Add((bytes, image.FileName));
                }

                var inputMediaPhotos = downloadedImages
                    // telegram allows photos up to 10 MB
                    .Where(x => x.Content.Length < 10 * 1024 * 1024)
                    .Select(x => new InputMediaPhoto(new InputFileStream(new MemoryStream(x.Content), x.FileName)))
                    .ToList();

                foreach (var mediaPhotoChunk in inputMediaPhotos.Chunk(10))
                {
                    if (mediaPhotoChunk.Length > 1)
                    {
                        await client.SendMediaGroup(
                            message.Chat.Id,
                            mediaPhotoChunk,
                            replyParameters: message);
                    }
                    else
                    {
                        await client.SendPhoto(
                            message.Chat.Id,
                            mediaPhotoChunk.First().Media,
                            replyParameters: message);
                    }
                }

                foreach (var image in downloadedImages)
                {
                    await client.SendDocument(
                        message.Chat.Id,
                        new InputFileStream(new MemoryStream(image.Content), image.FileName),
                        replyParameters: message);
                }
            }
        }
        catch (Exception e)
        {
            await client.SendMessage(
                message.Chat.Id,
                "Oops! Can't process your boosty post, please try again uwu 🍑" + "\n" + currentUrl + "\n" + e.Message,
                replyParameters: message);
        }
    }

    private static async Task DownloadTikTokAsync(ITelegramBotClient client, Message message, string id = null, int retry = 0)
    {
        try
        {
            TikTokDownloader.DownloadData tikTokData;
            if (id == null)
            {    
                var url = message.Text?.Split(" ").First(x => x.Contains("tiktok.com")) ?? "";
                tikTokData = await new TikTokDownloader().GetContentFromTikTokAsync(url);
            }
            else
            {
                tikTokData = await new TikTokDownloader().GetContentFromTikTokIdAsync(id);
            }

            if (tikTokData == null || !tikTokData.VideoList.Any() && !tikTokData.ImageList.Any())
            {
                await client.SendMessage(
                    message.Chat.Id,
                    "Oops! Can't process your video, please try again uwu 🍑",
                    replyParameters: message);
                return;
            }

            var sentByteLength = new List<long>();
            foreach (var video in tikTokData.VideoList)
            {
                using var ms = new MemoryStream();
                await using var mp4BytesStream = await new HttpClient().GetStreamAsync(video.Url);
                await mp4BytesStream.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);

                if (sentByteLength.Contains(ms.Length))
                    continue;
                
                sentByteLength.Add(ms.Length);

                await client.SendVideo(
                    message.Chat.Id,
                    new InputFileStream(ms, video.FileName),
                    replyParameters: message);
            }

            if (tikTokData.ImageList.Any())
            {
                var images = new List<(byte[] Content, string FileName)>();
                foreach (var image in tikTokData.ImageList)
                {
                    var bytes = await new HttpClient().GetByteArrayAsync(image.Url);
                    images.Add((bytes, image.FileName));
                }

                await client.SendMediaGroup(
                    message.Chat.Id,
                    images
                        .Select(x => new InputMediaPhoto(new InputFileStream(new MemoryStream(x.Content), x.FileName)))
                        .ToList(),
                    replyParameters: message);

                foreach (var image in images)
                {
                    await client.SendDocument(
                        message.Chat.Id,
                        new InputFileStream(new MemoryStream(image.Content), image.FileName),
                        replyParameters: message);
                }
            }
        }
        catch (Exception e)
        {
            if (retry < 3)
            {
                await DownloadTikTokAsync(client, message, id, retry + 1);
            }
            else
            {
                await client.SendMessage(
                    message.Chat.Id,
                    "Oops! Can't process your video, please try again uwu 🍑" + "\n" + e.Message,
                    replyParameters: message);
            }
        }
    }

    private static TelegramBotClient GetTelegramBotClient(ILogger logger)
    {
        var token = Environment.GetEnvironmentVariable("TelegramBotToken");
        if (token is not null) 
            return new TelegramBotClient(token);
        
        logger.LogError(string.Join("|||", Environment.GetEnvironmentVariables()));
        throw new Exception("Unable to read telegram bot token");
    }
}
