using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PhisicsGame.Core.Graphics;

/// <summary>
/// Загрузка PNG для всплывающих картинок: <see cref="Texture2D.FromStream"/> в DesktopGL иногда даёт
/// неправильную альфу (чёрный прямоугольник по размеру текстуры). ImageSharp читает RGBA из файла явно.
/// Экспорт из Photoroom и др. иногда даёт непрозрачный чёрный фон (A=255): тогда с краёв обходом удаляем
/// связную «тёмную» область, не трогая пиксели рядом с цветным содержимым (обводка, текст).
/// </summary>
public static class PortalPopupTextureLoader
{
    /// <summary>Максимум суммы RGB для «фонового» залива при обходе с края (чуть выше #000 для AA).</summary>
    private const int FloodDarkSumRgbMax = 18;

    /// <summary>Сосед считается «содержимым» (не фоном), если сумма RGB выше порога — кожа, белое облако, серая обводка.</summary>
    private const int ContentNeighborSumRgbMin = 28;

    public static Texture2D Load(GraphicsDevice graphicsDevice, string path, Texture2D fallbackPixel)
    {
        if (!File.Exists(path))
        {
            return fallbackPixel;
        }

        try
        {
            using var image = Image.Load<Rgba32>(path);
            if (image.Width < 2 || image.Height < 2)
            {
                return fallbackPixel;
            }

            var w = image.Width;
            var h = image.Height;
            var rgba = new Rgba32[w * h];
            image.CopyPixelDataTo(rgba);
            RemoveOpaqueBlackBackground(rgba, w, h);
            CropToOpaqueBounds(ref rgba, ref w, ref h);

            var texture = new Texture2D(graphicsDevice, w, h, false, SurfaceFormat.Color);
            var data = new Microsoft.Xna.Framework.Color[rgba.Length];
            for (var i = 0; i < rgba.Length; i++)
            {
                var p = rgba[i];
                data[i] = new Microsoft.Xna.Framework.Color(p.R, p.G, p.B, p.A);
            }

            texture.SetData(data);
            // Как у остальных PNG: SpriteBatch с AlphaBlend ожидает premultiplied (см. TextureHelper).
            TextureHelper.PremultiplyAlpha(texture);
            return texture;
        }
        catch
        {
            try
            {
                using var stream = File.OpenRead(path);
                var t = Texture2D.FromStream(graphicsDevice, stream);
                if (t.Width > 1)
                {
                    TextureHelper.PremultiplyAlpha(t);
                }

                return t;
            }
            catch
            {
                return fallbackPixel;
            }
        }
    }

    /// <summary>
    /// Обрезает поля по bounding box непрозрачных пикселей — чтобы при прижатии к низу экрана персонаж
    /// не «висел» из‑за пустого места внизу текстуры.
    /// </summary>
    private static void CropToOpaqueBounds(ref Rgba32[] rgba, ref int w, ref int h)
    {
        const byte alphaThreshold = 8;
        var minX = w;
        var minY = h;
        var maxX = -1;
        var maxY = -1;
        for (var y = 0; y < h; y++)
        {
            var row = y * w;
            for (var x = 0; x < w; x++)
            {
                if (rgba[row + x].A <= alphaThreshold)
                {
                    continue;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return;
        }

        var nw = maxX - minX + 1;
        var nh = maxY - minY + 1;
        var cropped = new Rgba32[nw * nh];
        for (var y = 0; y < nh; y++)
        {
            var srcRow = (minY + y) * w + minX;
            var dstRow = y * nw;
            Array.Copy(rgba, srcRow, cropped, dstRow, nw);
        }

        rgba = cropped;
        w = nw;
        h = nh;
    }

    /// <summary>
    /// Убирает непрозрачный чёрный фон: BFS с краёв по тёмным пикселям, затем удаляем только те
    /// достигнутые пиксели, у которых рядом нет «содержимого» (светлого/цветного соседа).
    /// </summary>
    private static void RemoveOpaqueBlackBackground(Rgba32[] rgba, int w, int h)
    {
        var n = w * h;
        var visited = new bool[n];
        var queue = new Queue<int>(Math.Min(n, 65536));

        bool IsFloodDark(Rgba32 p) =>
            p.A > 200 && p.R + p.G + p.B <= FloodDarkSumRgbMax;

        bool IsContentNeighbor(Rgba32 p) =>
            p.A > 200 && p.R + p.G + p.B >= ContentNeighborSumRgbMin;

        void TryEnqueue(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h)
            {
                return;
            }

            var i = y * w + x;
            if (visited[i]) return;
            if (!IsFloodDark(rgba[i])) return;
            visited[i] = true;
            queue.Enqueue(i);
        }

        for (var x = 0; x < w; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, h - 1);
        }

        for (var y = 0; y < h; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(w - 1, y);
        }

        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            var x = i % w;
            var y = i / w;
            TryEnqueue(x + 1, y);
            TryEnqueue(x - 1, y);
            TryEnqueue(x, y + 1);
            TryEnqueue(x, y - 1);
        }

        for (var i = 0; i < n; i++)
        {
            if (!visited[i])
            {
                continue;
            }

            var x = i % w;
            var y = i / w;
            var keep = false;
            for (var dy = -1; dy <= 1 && !keep; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (IsContentNeighbor(rgba[ny * w + nx]))
                    {
                        keep = true;
                        break;
                    }
                }
            }

            if (!keep)
            {
                rgba[i] = default;
            }
        }
    }
}
