using System.Text;
using LeezenPass.Api.Infrastructure.Images;
using SkiaSharp;

namespace LeezenPass.Api.Tests.Infrastructure;

public class ImageProcessorTests
{
  private readonly ImageProcessor _processor = new();

  [Fact]
  public void Strips_exif_including_gps()
  {
    var input = WithExif(RedBlueJpeg(40, 20), orientation: 1);
    Assert.True(ContainsAscii(input, "Exif"), "test input should carry EXIF");

    var result = _processor.Process(new MemoryStream(input));

    Assert.False(ContainsAscii(result.Full, "Exif"));
    Assert.False(ContainsAscii(result.Thumbnail, "Exif"));
    Assert.False(ContainsMarker(result.Full, 0xE1));
  }

  [Fact]
  public void Applies_exif_orientation_before_stripping_it()
  {
    // Orientation 6: the camera was held upright, pixels are stored rotated 90° counter-clockwise.
    var input = WithExif(RedBlueJpeg(40, 20), orientation: 6);

    var result = _processor.Process(new MemoryStream(input));

    Assert.Equal((20, 40), (result.Width, result.Height));
    using var output = SKBitmap.Decode(result.Full);
    Assert.Equal((20, 40), (output.Width, output.Height));
    // Left (red) half of the stored image ends up on top once rotated clockwise.
    Assert.True(IsRed(output.GetPixel(10, 5)));
    Assert.True(IsBlue(output.GetPixel(10, 35)));
  }

  [Fact]
  public void Resizes_to_max_edge_and_makes_thumbnail()
  {
    var result = _processor.Process(new MemoryStream(RedBlueJpeg(3200, 1000)));

    Assert.Equal((1600, 500), (result.Width, result.Height));
    using var thumbnail = SKBitmap.Decode(result.Thumbnail);
    Assert.Equal((400, 125), (thumbnail.Width, thumbnail.Height));
  }

  [Fact]
  public void Does_not_upscale_small_images()
  {
    var result = _processor.Process(new MemoryStream(RedBlueJpeg(300, 200)));

    Assert.Equal((300, 200), (result.Width, result.Height));
  }

  [Fact]
  public void Rejects_non_images()
  {
    Assert.Throws<InvalidImageException>(() =>
      _processor.Process(new MemoryStream("definitely not a jpeg"u8.ToArray())));
  }

  private static byte[] RedBlueJpeg(int width, int height)
  {
    using var bitmap = new SKBitmap(width, height);
    using (var canvas = new SKCanvas(bitmap))
    {
      canvas.Clear(SKColors.Blue);
      using var red = new SKPaint { Color = SKColors.Red };
      canvas.DrawRect(0, 0, width / 2f, height, red);
    }

    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
    return data.ToArray();
  }

  /// <summary>Inserts an APP1 EXIF segment with an orientation tag and a GPS IFD (latitude ref "N").</summary>
  private static byte[] WithExif(byte[] jpeg, ushort orientation)
  {
    var tiff = new List<byte>();
    void U16(int v) => tiff.AddRange([(byte)(v >> 8), (byte)v]);
    void U32(int v) => tiff.AddRange([(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v]);

    tiff.AddRange("MM"u8.ToArray()); U16(42); U32(8);             // big-endian TIFF header, IFD0 at 8
    U16(2);                                                        // IFD0: 2 entries
    U16(0x0112); U16(3); U32(1); U16(orientation); U16(0);         // Orientation (SHORT)
    U16(0x8825); U16(4); U32(1); U32(38);                          // GPS IFD pointer -> offset 38
    U32(0);                                                        // no next IFD
    U16(1);                                                        // GPS IFD: 1 entry
    U16(0x0001); U16(2); U32(2); tiff.AddRange("N\0\0\0"u8.ToArray()); // GPSLatitudeRef = "N"
    U32(0);

    var payload = "Exif\0\0"u8.ToArray().Concat(tiff).ToArray();
    var length = payload.Length + 2;
    byte[] app1 = [0xFF, 0xE1, (byte)(length >> 8), (byte)length, .. payload];

    // Right after SOI (FF D8).
    return [.. jpeg[..2], .. app1, .. jpeg[2..]];
  }

  private static bool ContainsAscii(byte[] bytes, string text) =>
    bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0;

  private static bool ContainsMarker(byte[] bytes, byte marker)
  {
    // Only scan the header segments (up to start-of-scan), where APPn markers live.
    for (var i = 2; i + 1 < bytes.Length && !(bytes[i] == 0xFF && bytes[i + 1] == 0xDA); i++)
    {
      if (bytes[i] == 0xFF && bytes[i + 1] == marker)
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsRed(SKColor c) => c.Red > 200 && c.Blue < 60;
  private static bool IsBlue(SKColor c) => c.Blue > 200 && c.Red < 60;
}
