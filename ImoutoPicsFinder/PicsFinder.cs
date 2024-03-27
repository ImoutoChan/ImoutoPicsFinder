using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IIqdbClient _iqdbClient;

    public PicsFinder(ITelegramBotClient telegramBotClient, IIqdbClient iqdbClient)
    {
        _telegramBotClient = telegramBotClient;
        _iqdbClient = iqdbClient;
    }

    public async Task ProcessPhotoPostAsync(Message message)
    {
        if (message.Photo == null || message.Photo.Length == 0)
            return;

        var fileId = message.Photo.Last().FileId;

        var replyMessage = await GetReplyMessageAsync(fileId);

        await _telegramBotClient.SendTextMessageAsync(
            chatId: message.Chat.Id, 
            text: replyMessage, 
            parseMode: ParseMode.Html,
            replyToMessageId: message.MessageId);
    }

    private async Task<string> GetReplyMessageAsync(string fileId)
    {
        using var ms = new MemoryStream();

        await _telegramBotClient.GetInfoAndDownloadFileAsync(fileId, ms);
        ms.Seek(0, SeekOrigin.Begin);

        var results = await _iqdbClient.SearchFile(ms);

        return BuildSearchResultMessage(results);
    }

    private static string BuildSearchResultMessage(SearchResult searchResults)
    {
        var sb = new StringBuilder();

        if (!searchResults.IsFound)
        {
            sb.AppendLine("🍰 similar images weren't found 😥");
            return sb.ToString();
        }

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
