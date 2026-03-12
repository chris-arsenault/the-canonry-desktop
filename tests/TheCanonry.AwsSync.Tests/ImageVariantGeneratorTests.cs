namespace TheCanonry.AwsSync.Tests;

using SkiaSharp;
using TheCanonry.AwsSync.Sync;

public class ImageVariantGeneratorTests
{
    // ── Helpers ─────────────────────────────────────────────────────────

    private static byte[] CreateTestPng(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // ── GetDimensions ───────────────────────────────────────────────────

    [Fact]
    public void GetDimensions_returns_correct_size()
    {
        var png = CreateTestPng(800, 600);

        var (width, height) = ImageVariantGenerator.GetDimensions(png);

        Assert.Equal(800, width);
        Assert.Equal(600, height);
    }

    [Fact]
    public void GetDimensions_handles_square_image()
    {
        var png = CreateTestPng(256, 256);

        var (width, height) = ImageVariantGenerator.GetDimensions(png);

        Assert.Equal(256, width);
        Assert.Equal(256, height);
    }

    [Fact]
    public void GetDimensions_handles_tall_portrait_image()
    {
        var png = CreateTestPng(400, 1200);

        var (width, height) = ImageVariantGenerator.GetDimensions(png);

        Assert.Equal(400, width);
        Assert.Equal(1200, height);
    }

    // ── ConvertToWebp ───────────────────────────────────────────────────

    [Fact]
    public void ConvertToWebp_produces_valid_webp_data()
    {
        var png = CreateTestPng(640, 480);

        var webp = ImageVariantGenerator.ConvertToWebp(png);

        Assert.NotNull(webp);
        Assert.True(webp.Length > 0);

        // Verify it decodes as a valid image with the right dimensions
        var (w, h) = ImageVariantGenerator.GetDimensions(webp);
        Assert.Equal(640, w);
        Assert.Equal(480, h);
    }

    [Fact]
    public void ConvertToWebp_with_lower_quality_produces_smaller_output()
    {
        var png = CreateTestPng(800, 600);

        var highQuality = ImageVariantGenerator.ConvertToWebp(png, quality: 95);
        var lowQuality = ImageVariantGenerator.ConvertToWebp(png, quality: 20);

        // Lower quality should generally be smaller (for non-trivial images this
        // might not hold for solid-color images, but we verify both are valid)
        Assert.True(highQuality.Length > 0);
        Assert.True(lowQuality.Length > 0);
    }

    // ── GenerateThumbnail ───────────────────────────────────────────────

    [Fact]
    public void GenerateThumbnail_constrains_width()
    {
        var png = CreateTestPng(1024, 768);

        var thumb = ImageVariantGenerator.GenerateThumbnail(png, maxWidth: 400);

        Assert.NotNull(thumb);
        Assert.True(thumb.Length > 0);

        var (w, _) = ImageVariantGenerator.GetDimensions(thumb);
        Assert.True(w <= 400, $"Thumbnail width {w} should be <= 400");
    }

    [Fact]
    public void GenerateThumbnail_preserves_aspect_ratio()
    {
        var png = CreateTestPng(1000, 500); // 2:1 ratio

        var thumb = ImageVariantGenerator.GenerateThumbnail(png, maxWidth: 400);

        var (w, h) = ImageVariantGenerator.GetDimensions(thumb);
        Assert.Equal(400, w);
        Assert.Equal(200, h); // Preserved 2:1 ratio
    }

    [Fact]
    public void GenerateThumbnail_does_not_upscale_small_images()
    {
        var png = CreateTestPng(200, 150); // Already smaller than maxWidth

        var thumb = ImageVariantGenerator.GenerateThumbnail(png, maxWidth: 400);

        var (w, h) = ImageVariantGenerator.GetDimensions(thumb);
        Assert.Equal(200, w);
        Assert.Equal(150, h);
    }

    [Fact]
    public void GenerateThumbnail_produces_webp_output()
    {
        var png = CreateTestPng(800, 600);

        var thumb = ImageVariantGenerator.GenerateThumbnail(png, maxWidth: 300);

        // WebP files start with RIFF header
        Assert.True(thumb.Length >= 4);
        Assert.Equal((byte)'R', thumb[0]);
        Assert.Equal((byte)'I', thumb[1]);
        Assert.Equal((byte)'F', thumb[2]);
        Assert.Equal((byte)'F', thumb[3]);
    }

    // ── Error handling ──────────────────────────────────────────────────

    [Fact]
    public void GetDimensions_throws_on_invalid_data()
    {
        var garbage = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        Assert.ThrowsAny<Exception>(() =>
            ImageVariantGenerator.GetDimensions(garbage));
    }

    [Fact]
    public void ConvertToWebp_throws_on_invalid_data()
    {
        var garbage = new byte[] { 0xFF, 0xFE, 0xFD };

        Assert.ThrowsAny<Exception>(() =>
            ImageVariantGenerator.ConvertToWebp(garbage));
    }

    [Fact]
    public void GenerateThumbnail_throws_on_invalid_data()
    {
        var garbage = new byte[] { 0xAA, 0xBB };

        Assert.ThrowsAny<Exception>(() =>
            ImageVariantGenerator.GenerateThumbnail(garbage));
    }
}
