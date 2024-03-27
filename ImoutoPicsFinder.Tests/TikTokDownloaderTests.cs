using FluentAssertions;
using ImoutoPicsFinder.TikTok;

namespace ImoutoPicsFinder.Tests;

public class TikTokDownloaderTests
{
    [Fact]
    public async Task TikTokDownloaderShouldDownloadVideoBytesFromLink()
    {
        var downloader = new TikTokDownloader();
        var video = await downloader.GetContentFromTikTokAsync("https://vt.tiktok.com/ZSFkwH1Je");
        var bytes = await new HttpClient().GetByteArrayAsync(video!.VideoList.First().Url);

        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(976595);
    }
    [Fact]
    public async Task TikTokDownloaderShouldDownloadVideoBytesFromLink2()
    {
        var downloader = new TikTokDownloader();
        var video = await downloader.GetContentFromTikTokAsync("https://vt.tiktok.com/ZSFQ4DuMj");
        var bytes = await new HttpClient().GetByteArrayAsync(video!.VideoList.First().Url);

        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(976595);
    }

    [Fact]
    public async Task TikTokDownloaderShouldDownloadVideoBytesFromId()
    {
        var downloader = new TikTokDownloader();
        var video = await downloader.GetContentFromTikTokIdAsync("7339599668361399557");
        var bytes = await new HttpClient().GetByteArrayAsync(video!.VideoList.First().Url);

        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(976595);
    }

    [Fact]
    public async Task TikTokDownloaderShouldDownloadVideoBytesFromId7327828687620050222()
    {
        var downloader = new TikTokDownloader();
        var video = await downloader.GetContentFromTikTokIdAsync("7327828687620050222");
        var bytes = await new HttpClient().GetByteArrayAsync(video!.VideoList.First().Url);

        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(976595);
    }
}
