using SkiaSharp;

namespace TheCanonry.AwsSync.Sync;

public static class ImageVariantGenerator
{
    public static byte[] ConvertToWebp(byte[] imageData, int quality = 85)
    {
        using var bitmap = SKBitmap.Decode(imageData)
            ?? throw new InvalidOperationException("Failed to decode image data");

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, quality);
        return encoded.ToArray();
    }

    public static byte[] GenerateThumbnail(byte[] imageData, int maxWidth = 400, int quality = 75)
    {
        using var bitmap = SKBitmap.Decode(imageData)
            ?? throw new InvalidOperationException("Failed to decode image data");

        if (bitmap.Width <= maxWidth)
        {
            // Image is already small enough; just convert to WebP
            using var smallImage = SKImage.FromBitmap(bitmap);
            using var smallEncoded = smallImage.Encode(SKEncodedImageFormat.Webp, quality);
            return smallEncoded.ToArray();
        }

        var scale = (float)maxWidth / bitmap.Width;
        var newWidth = maxWidth;
        var newHeight = (int)(bitmap.Height * scale);

        using var resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
        if (resized is null)
            throw new InvalidOperationException("Failed to resize image");

        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, quality);
        return encoded.ToArray();
    }

    public static (int Width, int Height) GetDimensions(byte[] imageData)
    {
        using var bitmap = SKBitmap.Decode(imageData)
            ?? throw new InvalidOperationException("Failed to decode image data");

        return (bitmap.Width, bitmap.Height);
    }
}
