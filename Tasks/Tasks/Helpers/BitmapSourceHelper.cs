using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhysicsTasks.Terminal.Helpers;

internal static class BitmapSourceHelper
{
    /// <summary>
    /// Pdfium и GDI+ могут отдавать кадры не в Format32bppArgb — прямой LockBits тогда падает.
    /// Рисуем во встраиваемый 32bpp ARGB буфер (белый фон под возможную прозрачность).
    /// </summary>
    public static BitmapSource FromDrawingImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var w = image.Width;
        var h = image.Height;
        if (w <= 0 || h <= 0)
            throw new ArgumentException("Некорректный размер изображения.", nameof(image));

        using var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.White);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(image, 0, 0, w, h);
        }

        return FromBitmap(bmp);
    }

    private static BitmapSource FromBitmap(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            var bs = BitmapSource.Create(
                bmp.Width,
                bmp.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                data.Scan0,
                data.Stride * bmp.Height,
                data.Stride);
            bs.Freeze();
            return bs;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }
}
