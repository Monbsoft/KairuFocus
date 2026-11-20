using System.Buffers.Binary;

namespace KairuFocus.Web.Tests.StaticSite;

/// <summary>The share image announced by <c>og:image</c>: the size the tags declare, light enough for every preview.</summary>
public sealed class ShareImageTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] Image = File.ReadAllBytes(StaticSiteFiles.PathOf("og-image.png"));

    [Fact]
    public void Should_Be1200By630Png_When_ReadingShareImage()
    {
        Assert.Equal(PngSignature, Image[..8]);

        // The IHDR chunk comes first: width then height, big-endian, at offsets 16 and 20.
        Assert.Equal(1200, BinaryPrimitives.ReadInt32BigEndian(Image.AsSpan(16, 4)));
        Assert.Equal(630, BinaryPrimitives.ReadInt32BigEndian(Image.AsSpan(20, 4)));
    }

    [Fact]
    public void Should_StayUnder300Kilobytes_When_ReadingShareImage()
    {
        Assert.True(Image.Length < 300 * 1024, $"og-image.png weighs {Image.Length / 1024} KB.");
    }
}
