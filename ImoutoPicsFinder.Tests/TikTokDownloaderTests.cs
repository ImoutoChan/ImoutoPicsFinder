using FluentAssertions;
using ImoutoPicsFinder.TikTok;
using Xunit;

namespace ImoutoPicsFinder.Tests;

public class TikTokDownloaderTests
{
    [Fact]
    public async Task TikTokDownloaderShouldDownloadVideoBytesFromLink()
    {
        var downloader = new TikTokDownloader();
        var video = await downloader.GetContentFromTikTokAsync("https://vt.tiktok.com/ZSYks7mnV");
        var bytes = await new HttpClient().GetByteArrayAsync(video!.VideoList.First().Url);

        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(2304588);
    }
    [Fact]
    public async Task TikTokDownloaderShouldDownloadImagesFromLink()
    {
        var downloader = new TikTokDownloader();
        var tiktok = await downloader.GetContentFromTikTokAsync("https://vt.tiktok.com/ZSYkWHN67");

        tiktok?.ImageList.Should().NotBeNull();
        tiktok!.ImageList.Should().HaveCount(5);
        
        var bytes = await new HttpClient().GetByteArrayAsync(tiktok.ImageList.First().Url);

        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(146594);
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
    public async Task TikTokDownloaderShouldDownloadVideoBytesFromId7376887901293923601()
    {
        var downloader = new TikTokDownloader();
        var video = await downloader.GetContentFromTikTokIdAsync("7376887901293923601");
        var bytes = await new HttpClient().GetByteArrayAsync(video!.VideoList.First().Url);

        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(2304588);
    }
}
