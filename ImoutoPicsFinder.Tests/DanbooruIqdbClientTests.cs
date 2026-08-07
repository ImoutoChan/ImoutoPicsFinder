using FluentAssertions;
using ImoutoPicsFinder.Danbooru;
using Xunit;

namespace ImoutoPicsFinder.Tests;

public class DanbooruIqdbClientTests
{
    [Fact]
    public async Task DanbooruIqdbShouldFindTheSourcePost()
    {
        var client = new DanbooruIqdbClient(Tokens.DanbooruLogin, Tokens.DanbooruApiKey);
        using var image = await DownloadAsync(
            "https://cdn.donmai.us/180x180/d3/4e/d34e4cf0a437a5d65f8e82b7bcd02606.jpg");

        var matches = await client.SearchFileAsync(image);

        matches.Should().NotBeEmpty();

        var best = matches.First();
        best.Post!.Id.Should().Be(1);
        best.Post.Width.Should().Be(459);
        best.Post.Height.Should().Be(650);
        best.Score.Should().BeGreaterThan(90);
    }

    [Fact]
    public async Task DanbooruIqdbShouldFindNothingForAnUnknownImage()
    {
        var client = new DanbooruIqdbClient(Tokens.DanbooruLogin, Tokens.DanbooruApiKey);
        using var image = await DownloadAsync(
            "https://upload.wikimedia.org/wikipedia/commons/4/47/PNG_transparency_demonstration_1.png");

        var matches = await client.SearchFileAsync(image);

        matches.Should().BeEmpty();
    }

    private static async Task<Stream> DownloadAsync(string url)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ImoutoPicsFinder/1.0");

        return new MemoryStream(await httpClient.GetByteArrayAsync(url));
    }
}
