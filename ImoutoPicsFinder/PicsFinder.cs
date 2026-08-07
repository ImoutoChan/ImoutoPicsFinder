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
    private readonly DanbooruIqdbClient _danbooruIqdbClient;

    public PicsFinder(
        ITelegramBotClient telegramBotClient,
        IIqdbClient iqdbClient,
        DanbooruIqdbClient danbooruIqdbClient)
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

        Exception iqdbFailure = null;
        try
        {
            var results = await _iqdbClient.SearchFile(new MemoryStream(image));

            if (results.IsFound)
            {
                // iqdb reports a hit even when every match is too weak to be shown
                var iqdbMessage = BuildSearchResultMessage(results);

                if (!string.IsNullOrWhiteSpace(iqdbMessage))
                    return iqdbMessage;
            }
        }
        catch (Exception e)
        {
            iqdbFailure = e;
        }

        // iqdb.org is either down or knows nothing about the picture, so ask danbooru's own iqdb
        if (_danbooruIqdbClient == null)
        {
            if (iqdbFailure != null)
                throw new Exception("iqdb: " + iqdbFailure.Message, iqdbFailure);

            return NotFoundMessage;
        }

        try
        {
            var danbooruMatches = await _danbooruIqdbClient.SearchFileAsync(new MemoryStream(image));

            return danbooruMatches.Any()
                ? BuildDanbooruSearchResultMessage(danbooruMatches)
                : NotFoundMessage;
        }
        catch (Exception e)
        {
            if (iqdbFailure != null)
                throw new Exception($"iqdb: {iqdbFailure.Message}, danbooru: {e.Message}", iqdbFailure);

            // iqdb worked and simply found nothing, danbooru was a bonus attempt, so don't fail over it
            return NotFoundMessage + "\n(danbooru search failed: " + e.Message + ")";
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
        sb.AppendLine("🍥 nothing on iqdb, but danbooru knows:");

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
