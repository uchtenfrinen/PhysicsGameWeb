using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhisicsGame.Gameplay;

/// <summary>
/// Персонаж: <c>Assets/Images/player.png</c> — горизонтальный спрайт-лист из <see cref="StripFrameCount"/> кадров в один ряд.
/// <para>
/// Рекомендуемый экспорт: PNG с прозрачностью (как после remove.bg), все кадры одной высоты,
/// одинаковая ширина ячеек (или близкая — тогда лучше подогнать в редакторе под общую сетку).
/// </para>
/// </summary>
public static class PlayerSprite
{
    /// <summary>Кадров в файле (полоска слева направо).</summary>
    public const int StripFrameCount = 5;

    /// <summary>
    /// Индекс кадра «стоит» (обычно 0 — лицом к камере). При <see cref="UseAllStripFramesForWalk"/> = true не используется для idle так же осмысленно.
    /// </summary>
    public const int IdleFrameIndex = 0;

    /// <summary>Первый кадр цикла ходьбы (для листа «idle + 4 шага в профиль» = 1).</summary>
    public const int WalkFirstFrameIndex = 1;

    /// <summary>Кадров в цикле бега/ходьбы (без idle). Для «1 idle + 4 walk» = 4.</summary>
    public const int WalkAnimFrameCount = 4;

    /// <summary>
    /// Если true — при движении крутятся все <see cref="StripFrameCount"/> кадров (0..4).
    /// Включите, если в листе нет отдельного idle, только 5 кадров шага.
    /// </summary>
    public static bool UseAllStripFramesForWalk { get; set; }

    /// <summary>Размер отрисовки в игровом мире (масштаб от реального кадра).</summary>
    public const int DrawWidth = 102;
    public const int DrawHeight = 118;

    /// <summary>Точка у «ног» для коллизий и камеры.</summary>
    public static Vector2 FootAnchor => new(DrawWidth * 0.5f, DrawHeight * 0.38f);

    /// <summary>Скорость смены кадров ходьбы (шагов индекса в секунду).</summary>
    public const float WalkAnimSpeed = 9.5f;

    /// <summary>Область кадра в текстуре (одна строка, равные доли по ширине).</summary>
    public static Rectangle GetSourceRect(Texture2D tex, int frameIndex)
    {
        if (tex.Width <= 1 || tex.Height <= 1)
        {
            return new Rectangle(0, 0, 1, 1);
        }

        frameIndex = (frameIndex % StripFrameCount + StripFrameCount) % StripFrameCount;
        var cellW = tex.Width / StripFrameCount;
        var x = frameIndex * cellW;
        // Последняя ячейка забирает остаток пикселей по ширине (на случай не делится нацело).
        var w = frameIndex == StripFrameCount - 1 ? tex.Width - x : cellW;
        var h = tex.Height;
        return new Rectangle(x, 0, w, h);
    }

    /// <summary>Какой кадр листа показывать в этом кадре игры.</summary>
    public static int ResolveFrameIndex(float runCycle, bool isMoving)
    {
        if (!isMoving)
        {
            return IdleFrameIndex;
        }

        if (UseAllStripFramesForWalk)
        {
            return ((int)runCycle % StripFrameCount + StripFrameCount) % StripFrameCount;
        }

        var mod = ((int)runCycle % WalkAnimFrameCount + WalkAnimFrameCount) % WalkAnimFrameCount;
        return WalkFirstFrameIndex + mod;
    }

    public static float AdvanceWalkCycle(float cycle, float dt, bool moving)
    {
        if (!moving)
        {
            return 0f;
        }

        return cycle + dt * WalkAnimSpeed;
    }

    /// <summary>
    /// Рисуем с привязкой к низу игрового прямоугольника (ноги), с масштабом под <see cref="DrawWidth"/>×<see cref="DrawHeight"/>.
    /// </summary>
    public static void Draw(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Texture2D fallbackPixel,
        Vector2 playerPos,
        float runCycle,
        bool isMoving,
        bool facingRight)
    {
        if (texture.Width <= 1 || texture.Height <= 1)
        {
            spriteBatch.Draw(
                fallbackPixel,
                new Rectangle((int)playerPos.X, (int)playerPos.Y, DrawWidth, DrawHeight),
                Color.CornflowerBlue);
            return;
        }

        var frameIndex = ResolveFrameIndex(runCycle, isMoving);
        var source = GetSourceRect(texture, frameIndex);

        var anchorWorld = new Vector2(
            playerPos.X + DrawWidth * 0.5f,
            playerPos.Y + DrawHeight);

        var origin = new Vector2(source.Width * 0.5f, source.Height);
        var scale = new Vector2(DrawWidth / (float)source.Width, DrawHeight / (float)source.Height);
        var fx = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        spriteBatch.Draw(texture, anchorWorld, source, Color.White, 0f, origin, scale, fx, 0f);
    }
}
