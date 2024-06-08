using FluentAssertions;
using ImoutoPicsFinder.Boosty;
using Xunit;

namespace ImoutoPicsFinder.Tests;

public class BoostyMessagesDownloaderTests
{
    [Fact]
    public async Task ShouldGetLinksFromMessageVideoUrl()
    {
        var url = $"https://boosty.to/app/messages/media/13763012/fa9cb5d7-46cc-46e9-ad84-b466dbf8f8cc?dialogId=555554&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();

        var links = await downloader.GetLinksAsync(url).ToListAsync();

        links.Should().HaveCount(1);
        links[0].Type.Should().Be(BoostyMediaType.Video);
        links[0].Url.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldGetLinksFromMessageImagesUrl()
    {
        var url = $"https://boosty.to/app/messages/media/13565331/6564b8b0-e870-40c5-ab0a-50780d2fa0f8?from=message&dialogId=555554&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();

        var links = await downloader.GetLinksAsync(url).ToListAsync();

        links.Should().HaveCount(2);
        links[0].Type.Should().Be(BoostyMediaType.Photo);
        links[0].Url.Should().NotBeNull();
        links[1].Type.Should().Be(BoostyMediaType.Photo);
        links[1].Url.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldGetLinksFromMessagePaywallUrl()
    {
        var url = $"https://boosty.to/app/messages/media/13512441/2a732a65-92eb-4c2e-894e-d906e55c15fb?dialogId=555554&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();

        var links = await downloader.GetLinksAsync(url).ToListAsync();

        links.Should().HaveCount(1);
        links[0].Type.Should().Be(BoostyMediaType.Video);
        links[0].Url.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ShouldDownloadFromMessageVideoUrl()
    {
        var url = $"https://boosty.to/app/messages/media/13763012/fa9cb5d7-46cc-46e9-ad84-b466dbf8f8cc?dialogId=555554&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();
        var links = await downloader.GetLinksAsync(url).ToListAsync();

        var bytes = await downloader.DownloadLinkAsync(links[0].Url);
        bytes.Should().HaveCount(58549481);
    }

    [Fact]
    public async Task ShouldDownloadFromMessageImagesUrl()
    {
        var url = $"https://boosty.to/app/messages/media/13565331/6564b8b0-e870-40c5-ab0a-50780d2fa0f8?from=message&dialogId=555554&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();

        var links = await downloader.GetLinksAsync(url).ToListAsync();

        var bytes = await downloader.DownloadLinkAsync(links[0].Url);
        bytes.Should().HaveCount(4960155);
    }

    [Fact]
    public async Task ShouldDownloadFromMessagePaywallUrl()
    {
        var url = $"https://boosty.to/app/messages/media/13512441/2a732a65-92eb-4c2e-894e-d906e55c15fb?dialogId=555554&auth={Tokens.BoostyAuthToken}";
        var downloader = new BoostyDownloader();

        var links = await downloader.GetLinksAsync(url).ToListAsync();

        var bytes = await downloader.DownloadLinkAsync(links[0].Url);
        bytes.Should().HaveCount(2975168);
    }
}