using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImoutoPicsFinder.Danbooru;
using IqdbApi;
using IqdbApi.Enums;
using IqdbApi.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MatchType = IqdbApi.Enums.MatchType;

namespace ImoutoPicsFinder;

public class PicsFinder
{
    private const string NotFoundMessage = "🍰 similar images weren't found 😥";

    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IIqdbClient _iqdbClient;
    private readonly IDanbooruIqdbClient _danbooruIqdbClient;

    public PicsFinder(
        ITelegramBotClient telegramBotClient,
        IIqdbClient iqdbClient,
        IDanbooruIqdbClient danbooruIqdbClient)
    {
        _telegramBotClient = telegramBotClient;
        _iqdbClient = iqdbClient;
        _danbooruIqdbClient = danbooruIqdbClient;
    }

    public async Task ProcessPhotoPostAsync(Message message)
    {
        if (message.Photo == null || message.Photo.Length == 0)
            return;

        var fileId = message.Photo.Last().FileId;

        var replyMessage = await GetReplyMessageAsync(fileId);

        await _telegramBotClient.SendMessage(
            chatId: message.Chat.Id, 
            text: replyMessage, 
            parseMode: ParseMode.Html,
            replyParameters: message.MessageId);
    }

    private async Task<string> GetReplyMessageAsync(string fileId)
    {
        using var ms = new MemoryStream();

        await _telegramBotClient.GetInfoAndDownloadFile(fileId, ms);

        // every search gets its own stream over the same bytes, the clients close what they're given
        var image = ms.ToArray();

        var iqdbTask = TrySearchAsync(image, async stream =>
        {
            var results = await _iqdbClient.SearchFile(stream);
            // iqdb reports a hit even when every match is too weak to be shown
            return results.IsFound ? BuildSearchResultMessage(results) : null;
        });
        var danbooruTask = _danbooruIqdbClient == null
            ? null
            : TrySearchAsync(image, async stream =>
            {
                var matches = await _danbooruIqdbClient.SearchFileAsync(stream);
                return matches.Any() ? BuildDanbooruSearchResultMessage(matches) : null;
            });

        if (danbooruTask != null)
        {
            var firstCompleted = await Task.WhenAny(iqdbTask, danbooruTask);
            var (firstMessage, _) = await firstCompleted;

            // The other search owns its stream and catches failures even after we return.
            if (!string.IsNullOrWhiteSpace(firstMessage))
                return firstMessage;
        }

        var (iqdbMessage, iqdbFailure) = await iqdbTask;

        if (!string.IsNullOrWhiteSpace(iqdbMessage))
            return iqdbMessage;

        if (danbooruTask == null)
        {
            if (iqdbFailure != null)
                throw new Exception("iqdb: " + iqdbFailure.Message, iqdbFailure);

            return NotFoundMessage;
        }

        var (danbooruMessage, danbooruFailure) = await danbooruTask;

        if (danbooruFailure == null)
            return danbooruMessage ?? NotFoundMessage;

        if (iqdbFailure != null)
            throw new Exception($"iqdb: {iqdbFailure.Message}, danbooru: {danbooruFailure.Message}", iqdbFailure);

        // iqdb worked and simply found nothing, danbooru was a bonus attempt, so don't fail over it
        return NotFoundMessage + "\n(danbooru search failed: " + danbooruFailure.Message + ")";
    }

    private static async Task<(string Message, Exception Failure)> TrySearchAsync(
        byte[] image, Func<Stream, Task<string>> search)
    {
        try
        {
            using var stream = new MemoryStream(image);
            return (await search(stream), null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }

    private static string BuildSearchResultMessage(SearchResult searchResults)
    {
        var sb = new StringBuilder();

        searchResults
            .Matches
            .Where(x => x.MatchType is MatchType.Best or MatchType.Additional)
            .OrderByDescending(x => x.Similarity / 10)
            .ThenBy(RankMatch)
            .ThenByDescending(x => x.Similarity)
            .ToList()
            .ForEach(x =>
                sb.AppendLine(
                    $"({x.Similarity} 🍑 {x.Resolution.Width}x{x.Resolution.Height} 🍒 {x.Url.Trim('/', '\\')}"));

        return sb.ToString();
    }

    private static string BuildDanbooruSearchResultMessage(IReadOnlyCollection<DanbooruIqdbMatch> matches)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🍥 danbooru knows:");

        foreach (var match in matches)
        {
            var score = match.Score.ToString("F1", CultureInfo.InvariantCulture);
            sb.AppendLine(
                $"({score} 🍑 {match.Post.Width}x{match.Post.Height} 🍒 danbooru.donmai.us/posts/{match.Post.Id}");
        }

        return sb.ToString();
    }

    private static int RankMatch(Match match)
        => match.Source switch
        {
            Source.Yandere => 1,
            Source.Danbooru => 0,
            Source.SankakuChannel => 2,
            Source.Konachan => 3,
            Source.Gelbooru => 3,
            _ => 4
        };
}
