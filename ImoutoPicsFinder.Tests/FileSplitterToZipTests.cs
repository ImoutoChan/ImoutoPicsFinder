using FluentAssertions;
using ImoutoPicsFinder.FileSplitter;
using Xunit;

namespace ImoutoPicsFinder.Tests;

public class FileSplitterToZipTests
{
    [Fact]
    public void Test()
    {
        var file = File.ReadAllBytes("test.mp4");
        
        var result = FileSplitterToZip.TrySplitFile(file, "test.mp4", out var splitted);

        result.Should().BeTrue();
        splitted.Should().HaveCount(2);
    }
}
