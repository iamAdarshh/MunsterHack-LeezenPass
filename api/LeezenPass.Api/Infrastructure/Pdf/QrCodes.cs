using QRCoder;

namespace LeezenPass.Api.Infrastructure.Pdf;

public static class QrCodes
{
  /// <summary>PNG bytes of a QR code (error correction M, 10 px per module).</summary>
  public static byte[] Png(string text)
  {
    using var generator = new QRCodeGenerator();
    using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
    return new PngByteQRCode(data).GetGraphic(10);
  }
}
