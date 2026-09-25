using SkiaSharp;

namespace LeezenPass.Api.Infrastructure.Images;

public sealed record ProcessedImage(byte[] Full, byte[] Thumbnail, int Width, int Height);

public class InvalidImageException(string message) : Exception(message);

/// <summary>
/// Every uploaded photo goes through here before it is stored: decode, apply EXIF orientation, redraw onto a
/// fresh bitmap and re-encode as JPEG. The output carries no metadata at all (no EXIF, no GPS).
/// </summary>
public class ImageProcessor
{
  public const int MaxEdge = 1600;
  public const int ThumbnailEdge = 400;
  public const long MaxInputPixels = 50_000_000;
  private const int JpegQuality = 85;

  public ProcessedImage Process(Stream input)
  {
    using var oriented = DecodeUpright(input);
    var (full, width, height) = EncodeJpeg(oriented, MaxEdge);
    var (thumbnail, _, _) = EncodeJpeg(oriented, ThumbnailEdge);

    return new ProcessedImage(full, thumbnail, width, height);
  }

  /// <summary>Single upright JPEG without metadata, e.g. for sending to the vision model.</summary>
  public byte[] ToJpeg(Stream input, int maxEdge)
  {
    using var oriented = DecodeUpright(input);
    return EncodeJpeg(oriented, maxEdge).Bytes;
  }

  private static SKBitmap DecodeUpright(Stream input)
  {
    using var data = SKData.Create(input) ?? throw new InvalidImageException("Unreadable image.");
    using var codec = SKCodec.Create(data) ?? throw new InvalidImageException("Unsupported image format.");
    if ((long)codec.Info.Width * codec.Info.Height > MaxInputPixels)
    {
      throw new InvalidImageException("Image is too large.");
    }

    using var decoded = SKBitmap.Decode(codec) ?? throw new InvalidImageException("Unreadable image.");
    return Orient(decoded, codec.EncodedOrigin);
  }

  /// <summary>Draws the pixels upright onto a new opaque bitmap (transparent areas become white).</summary>
  private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
  {
    var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
      or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
    var (w, h) = (source.Width, source.Height);

    var target = new SKBitmap(new SKImageInfo(swap ? h : w, swap ? w : h, SKColorType.Rgba8888, SKAlphaType.Opaque));
    using var canvas = new SKCanvas(target);
    canvas.Clear(SKColors.White);
    canvas.SetMatrix(OrientationMatrix(origin, w, h));
    canvas.DrawBitmap(source, 0, 0, SKSamplingOptions.Default);
    return target;
  }

  // Maps source (x, y) to upright (x', y'): x' = ScaleX*x + SkewX*y + TransX, y' = SkewY*x + ScaleY*y + TransY.
  private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, float w, float h) => origin switch
  {
    SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),     // mirror horizontally
    SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), // rotate 180°
    SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),   // mirror vertically
    SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),       // transpose
    SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),     // rotate 90° clockwise
    SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), // transverse
    SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),   // rotate 90° counter-clockwise
    _ => SKMatrix.Identity,
  };

  private static (byte[] Bytes, int Width, int Height) EncodeJpeg(SKBitmap bitmap, int maxEdge)
  {
    var scale = Math.Min(1d, (double)maxEdge / Math.Max(bitmap.Width, bitmap.Height));
    var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
    var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

    using var resized = scale < 1
      ? bitmap.Resize(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque),
          new SKSamplingOptions(SKCubicResampler.Mitchell))
      : bitmap.Copy();
    using var image = SKImage.FromBitmap(resized);
    using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
    return (encoded.ToArray(), width, height);
  }
}
