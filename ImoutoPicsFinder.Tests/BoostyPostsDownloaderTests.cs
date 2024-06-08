using FluentAssertions;
using ImoutoPicsFinder.Boosty;
using Xunit;

namespace ImoutoPicsFinder.Tests;

public class BoostyPostsDownloaderTests
{
    [Fact]
    public async Task ShouldGetLinksFromPostVideoUrl()
    {
        var url = $"https://boosty.to/xmednisx/posts/ffa2392d-215e-4b14-a61d-68f1bc9f1776?isFromFeed=true&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();

        var links = await downloader.GetLinksAsync(url).ToListAsync();

        links.Should().HaveCount(2);
        links[0].Type.Should().Be(BoostyMediaType.Video);
        links[0].Url.Should().NotBeNull();
        links[1].Type.Should().Be(BoostyMediaType.Photo);
        links[1].Url.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ShouldDownloadFromPostVideoUrl()
    {
        var url = $"https://boosty.to/xmednisx/posts/ffa2392d-215e-4b14-a61d-68f1bc9f1776?isFromFeed=true&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();
        var links = await downloader.GetLinksAsync(url).ToListAsync();

        var bytes = await downloader.DownloadLinkAsync(links[0].Url);
        bytes.Should().HaveCount(21948032);
    }
    
    [Fact]
    public async Task ShouldGetLinksFromPostPhotoUrl()
    {
        var url = $"https://boosty.to/app/feed/rawr99/posts/cb5c92f7-712e-4920-bddb-b20090ab8c4c/media/099bdb02-036a-4325-ba38-d8c809ed2f7f?auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();

        var links = await downloader.GetLinksAsync(url).ToListAsync();

        links.Should().HaveCount(7);
        links.ForEach(x =>
        {
            x.Type.Should().Be(BoostyMediaType.Photo);
            x.Url.Should().NotBeNull();
        });
    }
    
    [Fact]
    public async Task ShouldDownloadFromPostPhotoUrl()
    {
        var url = $"https://boosty.to/app/feed/rawr99/posts/cb5c92f7-712e-4920-bddb-b20090ab8c4c/media/099bdb02-036a-4325-ba38-d8c809ed2f7f?auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();
        var links = await downloader.GetLinksAsync(url).ToListAsync();

        var bytes = await downloader.DownloadLinkAsync(links[0].Url);
        bytes.Should().HaveCount(1719051);
    }
}
