using System;
using System.Threading.Tasks;
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
    
    private static TelegramBotClient GetTelegramBotClient(ILogger logger)
    {
        var token = Environment.GetEnvironmentVariable("TelegramBotToken");
        if (token is not null) 
            return new TelegramBotClient(token);
        
        logger.LogError(string.Join("|||", Environment.GetEnvironmentVariables()));
        throw new Exception("Unable to read telegram bot token");
    }
}
