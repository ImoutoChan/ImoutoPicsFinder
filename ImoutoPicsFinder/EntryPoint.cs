﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ImoutoPicsFinder.TikTok;
using IqdbApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ImoutoPicsFinder;

public static class EntryPoint
{
    [FunctionName("ImoutoPicsFinder")]
    public static async Task Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request,
        ILogger logger)
    {
        logger.LogInformation("PicsFinder function triggered");
        logger.LogInformation(request.GetDisplayUrl());

        var client = GetTelegramBotClient(logger);
        var picsFinder = new PicsFinder(client, new IqdbClient());

        var requestBody = await request.ReadAsStringAsync();
        var update = JsonConvert.DeserializeObject<Update>(requestBody);

        if (update.Message?.Text == "/start")
        {
            await client.SendTextMessageAsync(
                update.Message.Chat.Id,
                "Send me a picture and I will try to find the source! uwu! 🍑",
                replyToMessageId: update.Message.MessageId);
            return;
        }
        
        if (update.Message?.Text?.Contains("tiktok.com") == true)
        {
            await DownloadTikTokAsync(client, update.Message);
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
            await picsFinder.ProcessPhotoPostAsync(update.Message!);
        }
        catch (Exception e)
        {
            logger.LogError(e, "PicsFinder function failed" + e.Message);
            await client.SendTextMessageAsync(
                update.Message!.Chat.Id,
                "Oops! Can't process your picture, please try again uwu 🍑" + "\n" + e.Message,
                replyToMessageId: update.Message.MessageId);
        }
    }

    private static async Task DownloadTikTokAsync(TelegramBotClient client, Message message)
    {
        try
        {
            var url = message.Text?.Split(" ").First(x => x.Contains("tiktok.com")) ?? "";
            var tikTokData = await new TikTokDownloader().GetContentFromTikTokAsync(url);

            if (tikTokData == null || !tikTokData.VideoList.Any())
            {
                await client.SendTextMessageAsync(
                    message.Chat.Id,
                    "Oops! Can't process your video, please try again uwu 🍑",
                    replyToMessageId: message.MessageId);
                return;
            }
            
            foreach (var video in tikTokData.VideoList)
            {
                await using var mp4BytesStream = await new HttpClient().GetStreamAsync(video.Url);

                await client.SendVideoAsync(
                    message.Chat.Id,
                    new InputMedia(mp4BytesStream, video.FileName),
                    replyToMessageId: message.MessageId);
            }
        }
        catch (Exception e)
        {
            await client.SendTextMessageAsync(
                message.Chat.Id,
                "Oops! Can't process your video, please try again uwu 🍑" + "\n" + e.Message,
                replyToMessageId: message.MessageId);
            throw;
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
