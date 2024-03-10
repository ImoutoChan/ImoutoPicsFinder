using FluentAssertions;
using ImoutoPicsFinder.Instagram;

namespace ImoutoPicsFinder.Tests;

public class PaidInstagramDownloaderTests
{
    [Fact]
    public async Task InstagramDownloaderShouldGetReels()
    {
        var downloader = new PaidInstagramDownloader(Tokens.PaidInstagramToken);

        var content = await downloader.GetMedia("https://www.instagram.com/reel/CtxYDryMaG4");

        content.IsSuccess.Should().BeTrue();
        
        foreach (var item in content.Value.Files)
        {
            await File.WriteAllBytesAsync(item.Name, item.File);

            item.Type.Should().Be(InstagramMediaType.Video);
            item.File.Should().NotBeNull();
            item.File.Length.Should().Be(1642297);
        }
    }

    [Fact]
    public async Task InstagramDownloaderShouldGetReels2()
    {
        var downloader = new PaidInstagramDownloader(Tokens.PaidInstagramToken);

        var content = await downloader.GetMedia("https://www.instagram.com/reel/C4LgkSto5OS");

        content.IsSuccess.Should().BeTrue();
        
        foreach (var item in content.Value.Files)
        {
            await File.WriteAllBytesAsync(item.Name, item.File);

            item.Type.Should().Be(InstagramMediaType.Video);
            item.File.Should().NotBeNull();
            item.File.Length.Should().Be(4323604);
        }
    }

    [Fact]
    public async Task InstagramDownloaderShouldGetCarousel()
    {
        var downloader = new PaidInstagramDownloader(Tokens.PaidInstagramToken);

        var content = await downloader.GetMedia("https://www.instagram.com/p/C4UPpQlpFLk");

        content.IsSuccess.Should().BeTrue();
        
        foreach (var item in content.Value.Files)
        {
            await File.WriteAllBytesAsync(item.Name, item.File);

            item.Type.Should().Be(InstagramMediaType.Photo);
            item.File.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task InstagramDownloaderShouldGetPostVideo()
    {
        var downloader = new PaidInstagramDownloader(Tokens.PaidInstagramToken);

        var content = await downloader.GetMedia("https://www.instagram.com/p/C4GpAy3tA2u");

        content.IsSuccess.Should().BeTrue();
        
        foreach (var item in content.Value.Files)
        {
            await File.WriteAllBytesAsync(item.Name, item.File);

            item.Type.Should().Be(InstagramMediaType.Video);
            item.File.Should().NotBeNull();
            item.File.Length.Should().Be(5540119);
        }
    }

    [Fact]
    public async Task InstagramDownloaderShouldGetPostImage()
    {
        var downloader = new PaidInstagramDownloader(Tokens.PaidInstagramToken);

        var content = await downloader.GetMedia("https://www.instagram.com/p/C27M0mOiD_H/");

        content.IsSuccess.Should().BeTrue();
        
        foreach (var item in content.Value.Files)
        {
            await File.WriteAllBytesAsync(item.Name, item.File);

            item.Type.Should().Be(InstagramMediaType.Photo);
            item.File.Should().NotBeNull();
            item.File.Length.Should().Be(509446);
        }
    }

    [Fact]
    public async Task InstagramDownloaderShouldGetStoryPhoto()
    {
        var downloader = new PaidInstagramDownloader(Tokens.PaidInstagramToken);

        var content = await downloader.GetStory("https://www.instagram.com/stories/dodo_baby/3320009682909068463/");

        content.IsSuccess.Should().BeTrue();
        
        foreach (var item in content.Value.Files)
        {
            await File.WriteAllBytesAsync(item.Name, item.File);

            item.Type.Should().Be(InstagramMediaType.Photo);
            item.File.Should().NotBeNull();
            item.File.Length.Should().Be(462177);
        }
    }

    [Fact]
    public async Task InstagramDownloaderShouldGetStoryVideo()
    {
        var downloader = new PaidInstagramDownloader(Tokens.PaidInstagramToken);

        var content = await downloader.GetStory("https://www.instagram.com/stories/lmadinn/3319917254507963672/");

        content.IsSuccess.Should().BeTrue();
        
        foreach (var item in content.Value.Files)
        {
            await File.WriteAllBytesAsync(item.Name, item.File);

            item.Type.Should().Be(InstagramMediaType.Video);
            item.File.Should().NotBeNull();
            item.File.Length.Should().Be(917990);
        }
    }
}
