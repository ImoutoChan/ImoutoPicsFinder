using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using ImoutoPicsFinder.Danbooru;
using IqdbApi;
using IqdbApi.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Xunit;
using Match = IqdbApi.Models.Match;
using MatchType = IqdbApi.Enums.MatchType;

namespace ImoutoPicsFinder.Tests;

public class PicsFinderTests
{
    private const string IqdbUrl = "https://iqdb.example/image";
    private const string NotFound = "similar images weren't found";
    private static readonly byte[] Image = { 1, 2, 3, 4 };
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task StartsBothSearchesBeforeEitherCompletesWithIndependentStreams()
    {
        var iqdbStarted = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var danbooruStarted = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var telegram = new TelegramStub();
        var finder = new PicsFinder(telegram.Client,
            new IqdbStub(async stream =>
            {
                iqdbStarted.SetResult(stream);
                await finish.Task;
                return IqdbResult(false);
            }),
            new DanbooruStub(async stream =>
            {
                danbooruStarted.SetResult(stream);
                await finish.Task;
                return Array.Empty<DanbooruIqdbMatch>();
            }));

        var processing = finder.ProcessPhotoPostAsync(PhotoMessage());
        Stream[] streams;
        try
        {
            streams = await Task.WhenAll(iqdbStarted.Task, danbooruStarted.Task).WaitAsync(Timeout);
            Assert.False(processing.IsCompleted);
            Assert.NotSame(streams[0], streams[1]);
            foreach (var stream in streams)
            {
                var bytes = new byte[Image.Length];
                await stream.ReadExactlyAsync(bytes);
                Assert.Equal(Image, bytes);
            }
        }
        finally
        {
            finish.TrySetResult();
            await processing.WaitAsync(Timeout);
        }

        Assert.All(streams, stream => Assert.False(stream.CanRead));
        Assert.Contains(NotFound, telegram.Reply);
    }

    [Theory]
    [InlineData(Outcome.Found, Outcome.Failed, IqdbUrl)]
    [InlineData(Outcome.Empty, Outcome.Found, "danbooru.donmai.us/posts/1")]
    [InlineData(Outcome.Failed, Outcome.Found, "danbooru.donmai.us/posts/1")]
    [InlineData(Outcome.Empty, Outcome.Empty, NotFound)]
    [InlineData(Outcome.Failed, Outcome.Empty, NotFound)]
    [InlineData(Outcome.Empty, Outcome.Failed, "danbooru search failed: danbooru unavailable")]
    [InlineData(Outcome.Found, Outcome.Disabled, IqdbUrl)]
    [InlineData(Outcome.Empty, Outcome.Disabled, NotFound)]
    public async Task HandlesProviderResultsAndFailures(
        Outcome iqdb, Outcome danbooru, string expected)
    {
        using var telegram = new TelegramStub();
        var finder = CreateFinder(telegram.Client, iqdb, danbooru);

        await finder.ProcessPhotoPostAsync(PhotoMessage());

        Assert.Contains(expected, telegram.Reply);
        if (iqdb == Outcome.Found)
            Assert.DoesNotContain("danbooru", telegram.Reply);
    }

    [Theory]
    [InlineData(Outcome.Failed, "iqdb: iqdb unavailable, danbooru: danbooru unavailable")]
    [InlineData(Outcome.Disabled, "iqdb: iqdb unavailable")]
    public async Task ReportsSearchFailuresWhenNeitherProviderCanRespond(Outcome danbooru, string expected)
    {
        using var telegram = new TelegramStub();
        var finder = CreateFinder(telegram.Client, Outcome.Failed, danbooru);

        var error = await Assert.ThrowsAsync<Exception>(() => finder.ProcessPhotoPostAsync(PhotoMessage()));

        Assert.Equal(expected, error.Message);
        Assert.Equal("iqdb unavailable", error.InnerException?.Message);
        Assert.Null(telegram.Reply);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task ReturnsFirstMatchWhileOtherSearchIsStillPending(bool iqdbFirst, bool otherFails)
    {
        var iqdb = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var danbooru = new TaskCompletionSource<IReadOnlyCollection<DanbooruIqdbMatch>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var telegram = new TelegramStub();
        var finder = new PicsFinder(telegram.Client, new IqdbStub(_ => iqdb.Task), new DanbooruStub(_ => danbooru.Task));
        var processing = finder.ProcessPhotoPostAsync(PhotoMessage());
        try
        {
            if (iqdbFirst)
                iqdb.SetResult(IqdbResult(true));
            else
                danbooru.SetResult(DanbooruMatches());

            await processing.WaitAsync(Timeout);

            Assert.False(iqdbFirst ? danbooru.Task.IsCompleted : iqdb.Task.IsCompleted);
            Assert.Equal(1, telegram.ReplyCount);
            Assert.Contains(iqdbFirst ? IqdbUrl : "danbooru.donmai.us/posts/1", telegram.Reply);
            Assert.DoesNotContain("nothing on iqdb", telegram.Reply);
        }
        finally
        {
            if (otherFails)
            {
                if (iqdbFirst)
                    danbooru.TrySetException(new Exception("late danbooru failure"));
                else
                    iqdb.TrySetException(new Exception("late iqdb failure"));
            }
            else
            {
                iqdb.TrySetResult(IqdbResult(true));
                danbooru.TrySetResult(DanbooruMatches());
            }
            await processing.WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task WaitsForOtherSearchWhenFirstResponseHasNoMatches(bool iqdbFirst, bool firstFails)
    {
        var iqdb = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var danbooru = new TaskCompletionSource<IReadOnlyCollection<DanbooruIqdbMatch>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var telegram = new TelegramStub();
        var finder = new PicsFinder(telegram.Client,
            new IqdbStub(_ => iqdbFirst
                ? firstFails ? throw new Exception("iqdb unavailable") : Task.FromResult(IqdbResult(false))
                : iqdb.Task),
            new DanbooruStub(_ => !iqdbFirst
                ? firstFails ? throw new Exception("danbooru unavailable")
                    : Task.FromResult<IReadOnlyCollection<DanbooruIqdbMatch>>(Array.Empty<DanbooruIqdbMatch>())
                : danbooru.Task));
        var processing = finder.ProcessPhotoPostAsync(PhotoMessage());
        try
        {
            Assert.False(processing.IsCompleted);
            Assert.Null(telegram.Reply);
        }
        finally
        {
            iqdb.TrySetResult(IqdbResult(true));
            danbooru.TrySetResult(DanbooruMatches());
        }

        await processing.WaitAsync(Timeout);

        Assert.Equal(1, telegram.ReplyCount);
        Assert.Contains(iqdbFirst ? "danbooru.donmai.us/posts/1" : IqdbUrl, telegram.Reply);
    }

    private static IReadOnlyCollection<DanbooruIqdbMatch> DanbooruMatches()
        => new[] { new DanbooruIqdbMatch(1, 95, new DanbooruIqdbPost(1, 459, 650)) };

    private static PicsFinder CreateFinder(ITelegramBotClient telegram, Outcome iqdb, Outcome danbooru)
        => new(telegram,
            new IqdbStub(_ => iqdb == Outcome.Failed
                ? throw new Exception("iqdb unavailable")
                : Task.FromResult(IqdbResult(iqdb == Outcome.Found))),
            danbooru == Outcome.Disabled ? null : new DanbooruStub(_ => danbooru == Outcome.Failed
                ? throw new Exception("danbooru unavailable")
                : Task.FromResult<IReadOnlyCollection<DanbooruIqdbMatch>>(danbooru == Outcome.Found
                    ? DanbooruMatches()
                    : Array.Empty<DanbooruIqdbMatch>())));

    private static SearchResult IqdbResult(bool found)
    {
        var matches = ImmutableList<Match>.Empty;
        if (found)
        {
            var match = new Match();
            SetProperty(match, nameof(Match.MatchType), MatchType.Best);
            SetProperty(match, nameof(Match.Url), IqdbUrl);
            SetProperty(match, nameof(Match.Similarity), (byte)95);
            matches = matches.Add(match);
        }

        var result = new SearchResult();
        SetProperty(result, nameof(SearchResult.Matches), matches);
        return result;
    }

    // IqdbApi exposes its response models with non-public setters.
    private static void SetProperty<T>(T target, string name, object value)
        => typeof(T).GetProperty(name)!.SetValue(target, value);

    private static Message PhotoMessage()
        => new()
        {
            Id = 1,
            Chat = new Chat { Id = 1 },
            Photo = new[] { new PhotoSize { FileId = "image", FileUniqueId = "image" } }
        };

    public enum Outcome { Found, Empty, Failed, Disabled }

    private sealed class IqdbStub(Func<Stream, Task<SearchResult>> search) : IIqdbClient
    {
        public Task<SearchResult> SearchFile(Stream file, CancellationToken cancellationToken = default)
            => search(file);

        public void Dispose() { }

        public Task<SearchResult> SearchUrl(string url, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class DanbooruStub(Func<Stream, Task<IReadOnlyCollection<DanbooruIqdbMatch>>> search)
        : IDanbooruIqdbClient
    {
        public Task<IReadOnlyCollection<DanbooruIqdbMatch>> SearchFileAsync(Stream file) => search(file);
    }

    private sealed class TelegramStub : HttpMessageHandler
    {
        private readonly HttpClient _httpClient;
        public TelegramBotClient Client { get; }
        public string? Reply { get; private set; }
        public int ReplyCount { get; private set; }

        public TelegramStub()
        {
            _httpClient = new HttpClient(this, disposeHandler: false);
            Client = new TelegramBotClient("123456:test", _httpClient);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            HttpContent content;
            if (path.EndsWith("/getFile"))
            {
                content = Json("""{"ok":true,"result":{"file_id":"image","file_unique_id":"image","file_path":"photo.jpg"}}""");
            }
            else if (path.EndsWith("/photo.jpg"))
            {
                content = new ByteArrayContent(Image);
            }
            else if (path.EndsWith("/sendMessage"))
            {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                Reply = body.RootElement.GetProperty("text").GetString();
                ReplyCount++;
                content = Json("""{"ok":true,"result":{"message_id":2,"date":0,"chat":{"id":1,"type":"private"}}}""");
            }
            else
            {
                throw new InvalidOperationException("Unexpected Telegram request: " + path);
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }

        private static StringContent Json(string value) => new(value, Encoding.UTF8, "application/json");

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _httpClient.Dispose();
            base.Dispose(disposing);
        }
    }
}
