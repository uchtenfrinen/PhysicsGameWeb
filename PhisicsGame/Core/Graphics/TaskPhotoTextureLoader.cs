using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PhisicsGame.Core.Graphics;

/// <summary>
/// Загрузка фото задачи: <see cref="Texture2D.FromStream"/> на WindowsDX не всегда читает JPEG/WebP;
/// ImageSharp декодирует в RGBA, затем текстура с premultiplied alpha для SpriteBatch.
/// </summary>
public static class TaskPhotoTextureLoader
{
    public static Texture2D? TryLoad(GraphicsDevice graphicsDevice, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            using var image = Image.Load<Rgba32>(fullPath);
            if (image.Width < 1 || image.Height < 1)
            {
                return null;
            }

            var w = image.Width;
            var h = image.Height;
            var rgba = new Rgba32[w * h];
            image.CopyPixelDataTo(rgba);
            var texture = new Texture2D(graphicsDevice, w, h, false, SurfaceFormat.Color);
            var data = new Microsoft.Xna.Framework.Color[rgba.Length];
            for (var i = 0; i < rgba.Length; i++)
            {
                var p = rgba[i];
                data[i] = new Microsoft.Xna.Framework.Color(p.R, p.G, p.B, p.A);
            }

            texture.SetData(data);
            TextureHelper.PremultiplyAlpha(texture);
            return texture;
        }
        catch
        {
            try
            {
                using var stream = File.OpenRead(fullPath);
                var texture = Texture2D.FromStream(graphicsDevice, stream);
                if (texture.Width >= 1 && texture.Height >= 1)
                {
                    TextureHelper.PremultiplyAlpha(texture);
                }

                return texture;
            }
            catch
            {
                return null;
            }
        }
    }
}
