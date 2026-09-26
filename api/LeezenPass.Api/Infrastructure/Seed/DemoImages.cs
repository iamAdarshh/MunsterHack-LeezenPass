using LeezenPass.Api.Domain.Bikes;
using SkiaSharp;

namespace LeezenPass.Api.Infrastructure.Seed;

/// <summary>Look of a drawn demo bike.</summary>
public sealed record BikeLook(BikeType Type, string ColorPrimary, string? ColorSecondary, bool Basket, bool Rack, bool Fenders, bool IsEbike);

/// <summary>
/// Synthetic photos for seed data, drawn with SkiaSharp: no real person's bike, receipt or location ends up in the demo.
/// Every image carries a "Demo-Daten" watermark.
/// </summary>
public static class DemoImages
{
  private const int JpegQuality = 88;

  public static byte[] Bike(BikeLook look, int variant)
  {
    const int w = 1200, h = 800;
    return Draw(w, h, canvas =>
    {
      // Background: sky-ish wall, pavement, a bit of variation per bike.
      var hue = variant % 360;
      using (var bg = new SKPaint())
      {
        bg.Shader = SKShader.CreateLinearGradient(
          new SKPoint(0, 0), new SKPoint(0, h),
          [SKColor.FromHsl(hue, 25, 88), SKColor.FromHsl(hue, 15, 78)], SKShaderTileMode.Clamp);
        canvas.DrawRect(0, 0, w, h, bg);
      }

      using (var ground = Fill(new SKColor(0x9c, 0xa3, 0xaf)))
      {
        canvas.DrawRect(0, 640, w, h - 640, ground);
      }

      var frame = ColorOf(look.ColorPrimary);
      var accent = look.ColorSecondary is { } second ? ColorOf(second) : frame.WithAlpha(255);
      var small = look.Type is BikeType.Kids or BikeType.Folding;
      var r = small ? 120f : 170f;
      var rear = new SKPoint(small ? 400 : 330, 640 - r);
      var front = new SKPoint(small ? 800 : 870, 640 - r);
      if (look.Type == BikeType.Cargo)
      {
        front = front with { X = 960 };
      }

      Wheel(canvas, rear, r);
      Wheel(canvas, front, r);

      var bottom = new SKPoint(rear.X + ((front.X - rear.X) * 0.42f), rear.Y + 10);
      var seatTop = new SKPoint(bottom.X - 55, bottom.Y - (r * 1.55f));
      var headTop = new SKPoint(front.X - (r * 0.45f), seatTop.Y + 15);
      var headBottom = new SKPoint(front.X - (r * 0.32f), bottom.Y - (r * 0.55f));
      var stepThrough = look.Type is BikeType.City or BikeType.Cargo or BikeType.Kids;

      using var tube = Stroke(frame, 22);
      canvas.DrawLine(bottom, seatTop, tube);
      canvas.DrawLine(bottom, stepThrough ? headBottom : headTop, tube);
      canvas.DrawLine(stepThrough ? new SKPoint(bottom.X + 20, bottom.Y - 70) : seatTop, stepThrough ? headBottom : headTop, tube);
      canvas.DrawLine(headTop, headBottom, tube);
      using var stay = Stroke(accent, 14);
      canvas.DrawLine(rear, bottom, stay);
      canvas.DrawLine(rear, new SKPoint(seatTop.X + 10, seatTop.Y + 30), stay);
      using var fork = Stroke(accent, 16);
      canvas.DrawLine(headBottom, front, fork);

      if (look.Type == BikeType.Cargo)
      {
        using var box = Fill(new SKColor(0x7c, 0x5a, 0x3a));
        canvas.DrawRoundRect(new SKRect(headBottom.X + 10, front.Y - 150, front.X - 20, front.Y - 10), 12, 12, box);
      }

      // Saddle, seat post, handlebar.
      using var dark = Stroke(new SKColor(0x1f, 0x29, 0x37), 14);
      canvas.DrawLine(seatTop, new SKPoint(seatTop.X - 8, seatTop.Y - 45), dark);
      using (var saddle = Fill(new SKColor(0x1f, 0x29, 0x37)))
      {
        canvas.DrawRoundRect(new SKRect(seatTop.X - 70, seatTop.Y - 62, seatTop.X + 40, seatTop.Y - 40), 10, 10, saddle);
      }

      var barTop = new SKPoint(headTop.X - 10, headTop.Y - 60);
      canvas.DrawLine(headTop, barTop, dark);
      if (look.Type == BikeType.Road || look.Type == BikeType.Gravel)
      {
        using var drop = Stroke(new SKColor(0x1f, 0x29, 0x37), 12);
        drop.Style = SKPaintStyle.Stroke;
        canvas.DrawArc(new SKRect(barTop.X - 5, barTop.Y - 5, barTop.X + 70, barTop.Y + 70), -90, 180, false, drop);
      }
      else
      {
        canvas.DrawLine(barTop, new SKPoint(barTop.X - 70, barTop.Y - 10), dark);
      }

      if (look.Rack)
      {
        using var rack = Stroke(new SKColor(0x37, 0x41, 0x51), 10);
        canvas.DrawLine(new SKPoint(rear.X - 90, rear.Y - r + 10), new SKPoint(seatTop.X - 10, rear.Y - r + 10), rack);
        canvas.DrawLine(new SKPoint(rear.X - 60, rear.Y - r + 10), rear, rack);
      }

      if (look.Fenders)
      {
        using var fender = Stroke(new SKColor(0x4b, 0x55, 0x63), 9);
        canvas.DrawArc(new SKRect(rear.X - r - 12, rear.Y - r - 12, rear.X + r + 12, rear.Y + r + 12), 190, 140, false, fender);
        canvas.DrawArc(new SKRect(front.X - r - 12, front.Y - r - 12, front.X + r + 12, front.Y + r + 12), 210, 140, false, fender);
      }

      if (look.Basket && look.Type != BikeType.Cargo)
      {
        using var basket = Stroke(new SKColor(0x92, 0x40, 0x0e), 8);
        canvas.DrawRect(new SKRect(barTop.X + 5, barTop.Y + 10, barTop.X + 115, barTop.Y + 90), basket);
      }

      if (look.IsEbike)
      {
        using var battery = Fill(new SKColor(0x11, 0x18, 0x27));
        var mid = new SKPoint((bottom.X + headBottom.X) / 2, (bottom.Y + headBottom.Y) / 2);
        canvas.Save();
        canvas.RotateDegrees(-18, mid.X, mid.Y);
        canvas.DrawRoundRect(new SKRect(mid.X - 70, mid.Y - 38, mid.X + 70, mid.Y - 8), 8, 8, battery);
        canvas.Restore();
      }

      Watermark(canvas, w, h);
    });
  }

  /// <summary>Sales receipt. <paramref name="frameNumber"/> null draws a receipt without any bike on it.</summary>
  public static byte[] Receipt(string? brand, string? model, string? frameNumber, DateOnly date, string total)
  {
    const int w = 900, h = 1200;
    return Draw(w, h, canvas =>
    {
      canvas.Clear(new SKColor(0xe5, 0xe7, 0xeb));
      using (var paper = Fill(SKColors.White))
      {
        canvas.DrawRect(120, 60, w - 240, h - 120, paper);
      }

      var y = 150f;
      void Line(string text, float size = 30, bool bold = false, SKTextAlign align = SKTextAlign.Left)
      {
        using var font = Font(size, bold);
        using var ink = Fill(new SKColor(0x11, 0x18, 0x27));
        var x = align == SKTextAlign.Center ? w / 2f : 160f;
        canvas.DrawText(text, x, y, align, font, ink);
        y += size * 1.6f;
      }

      if (frameNumber is null)
      {
        Line("Wochenmarkt Domplatz", 40, true, SKTextAlign.Center);
        Line("Münster", 28, false, SKTextAlign.Center);
        y += 40;
        Line($"Datum: {date:dd.MM.yyyy}");
        y += 20;
        Line("Äpfel 2 kg              5,80 €");
        Line("Brot                    4,20 €");
        Line("Käse                    7,50 €");
      }
      else
      {
        Line("Fahrradhaus Musterstadt", 40, true, SKTextAlign.Center);
        Line("Hammer Str. 1 · Münster", 28, false, SKTextAlign.Center);
        y += 40;
        Line($"Rechnung Nr. DEMO-{date:yyMMdd}");
        Line($"Datum: {date:dd.MM.yyyy}");
        y += 20;
        Line($"1x {brand} {model}", 32, true);
        Line($"Rahmennummer: {frameNumber}", 32, true);
        Line("Schloss, Montage");
      }

      y += 30;
      Line($"Summe: {total}", 36, true);
      y += 40;
      Line("Demo-Daten – kein echter Beleg", 26, false, SKTextAlign.Center);
      Watermark(canvas, w, h);
    });
  }

  /// <summary>Close-up of a stamped frame number with a note card next to it (possession check).</summary>
  public static byte[] FrameNumberCloseUp(string frameNumber, string cardText)
  {
    const int w = 1200, h = 800;
    return Draw(w, h, canvas =>
    {
      using (var metal = new SKPaint())
      {
        metal.Shader = SKShader.CreateLinearGradient(
          new SKPoint(0, 0), new SKPoint(w, h),
          [new SKColor(0x4b, 0x55, 0x63), new SKColor(0x9c, 0xa3, 0xaf), new SKColor(0x37, 0x41, 0x51)],
          SKShaderTileMode.Clamp);
        canvas.DrawRect(0, 0, w, h, metal);
      }

      using (var font = Font(96, true))
      using (var stamp = Fill(new SKColor(0x1f, 0x29, 0x37)))
      {
        canvas.DrawText(frameNumber, w / 2f, 300, SKTextAlign.Center, font, stamp);
      }

      using (var card = Fill(new SKColor(0xfe, 0xf9, 0xc3)))
      {
        canvas.DrawRoundRect(new SKRect(350, 420, 850, 640), 16, 16, card);
      }

      using (var font = Font(72, true))
      using (var pen = Fill(new SKColor(0x1d, 0x4e, 0xd8)))
      {
        canvas.DrawText(cardText, w / 2f, 560, SKTextAlign.Center, font, pen);
      }

      Watermark(canvas, w, h);
    });
  }

  private static byte[] Draw(int width, int height, Action<SKCanvas> paint)
  {
    using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque));
    using (var canvas = new SKCanvas(bitmap))
    {
      paint(canvas);
    }

    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
    return data.ToArray();
  }

  private static void Wheel(SKCanvas canvas, SKPoint centre, float radius)
  {
    using var tyre = Stroke(new SKColor(0x11, 0x18, 0x27), 20);
    canvas.DrawCircle(centre, radius, tyre);
    using var spoke = Stroke(new SKColor(0x6b, 0x72, 0x80), 3);
    for (var i = 0; i < 16; i++)
    {
      var angle = i * Math.PI / 8;
      canvas.DrawLine(centre, new SKPoint(centre.X + (float)(Math.Cos(angle) * radius), centre.Y + (float)(Math.Sin(angle) * radius)), spoke);
    }

    using var hub = Fill(new SKColor(0x37, 0x41, 0x51));
    canvas.DrawCircle(centre, 14, hub);
  }

  private static void Watermark(SKCanvas canvas, int width, int height)
  {
    using var font = Font(Math.Min(width, height) / 8f, true);
    using var paint = Fill(new SKColor(0xdc, 0x26, 0x26, 0x55));
    canvas.Save();
    canvas.RotateDegrees(-20, width / 2f, height / 2f);
    canvas.DrawText("Demo-Daten", width / 2f, height / 2f, SKTextAlign.Center, font, paint);
    canvas.Restore();

    using var label = Font(28, true);
    using var labelBg = Fill(new SKColor(0xdc, 0x26, 0x26));
    using var labelInk = Fill(SKColors.White);
    canvas.DrawRect(0, 0, 210, 48, labelBg);
    canvas.DrawText("DEMO-DATEN", 105, 34, SKTextAlign.Center, label, labelInk);
  }

  private static SKFont Font(float size, bool bold) =>
    new(SKTypeface.FromFamilyName("Helvetica", bold ? SKFontStyle.Bold : SKFontStyle.Normal), size);

  private static SKPaint Fill(SKColor color) => new() { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };

  private static SKPaint Stroke(SKColor color, float width) =>
    new() { Color = color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width, StrokeCap = SKStrokeCap.Round };

  private static SKColor ColorOf(string key) => key switch
  {
    "black" => new SKColor(0x1f, 0x1f, 0x1f),
    "white" => new SKColor(0xf5, 0xf5, 0xf5),
    "grey" => new SKColor(0x6b, 0x72, 0x80),
    "silver" => new SKColor(0xc0, 0xc4, 0xcc),
    "red" => new SKColor(0xdc, 0x26, 0x26),
    "blue" => new SKColor(0x25, 0x63, 0xeb),
    "green" => new SKColor(0x16, 0xa3, 0x4a),
    "yellow" => new SKColor(0xfa, 0xcc, 0x15),
    "orange" => new SKColor(0xf9, 0x73, 0x16),
    "brown" => new SKColor(0x92, 0x40, 0x0e),
    "beige" => new SKColor(0xd6, 0xc7, 0xa1),
    "pink" => new SKColor(0xec, 0x48, 0x99),
    "purple" => new SKColor(0x93, 0x33, 0xea),
    _ => new SKColor(0x0d, 0x94, 0x88),
  };
}
