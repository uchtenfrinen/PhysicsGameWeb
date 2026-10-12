using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core;
using PhisicsGame.Core.Scenes;
using PhisicsGame.Core.Text;
using PhisicsGame.Core.UI;

namespace PhisicsGame.Gameplay.Scenes;

/// <summary>Портал и подпись «Загрузка» + название перед входом в мир (первый мир или тема из хаба).</summary>
public sealed class WorldLoadingScene(
    SceneManager sceneManager,
    GameServices services,
    Func<IScene> nextSceneFactory,
    string loadingTargetLabel) : IScene
{
    private float _elapsed;
    private const float MinDisplaySeconds = 1.45f;

    public void OnEnter()
    {
        _elapsed = 0f;
    }

    public void OnExit()
    {
    }

    public void Update(GameTime gameTime)
    {
        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_elapsed >= MinDisplaySeconds)
        {
            sceneManager.ChangeScene(nextSceneFactory());
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        const int vw = 1280;
        const int vh = 720;
        const int textReserve = 120;
        const int maxPortalW = 280;
        const int topMargin = 24;

        var portal = services.WorldLoadingPortalTexture;
        var maxPortalH = Math.Max(64, vh - textReserve - topMargin);
        var scaleW = portal.Width > 0 ? maxPortalW / (float)portal.Width : 1f;
        var scaleH = portal.Height > 0 ? maxPortalH / (float)portal.Height : 1f;
        var scale = Math.Min(1f, Math.Min(scaleW, scaleH));
        var drawW = Math.Max(1, (int)(portal.Width * scale));
        var drawH = Math.Max(1, (int)(portal.Height * scale));
        var x = (vw - drawW) / 2;
        var y = topMargin + (vh - textReserve - topMargin - drawH) / 2;

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        spriteBatch.Draw(services.Pixel, new Rectangle(0, 0, vw, vh), Color.Black);
        spriteBatch.Draw(portal, new Rectangle(x, y, drawW, drawH), Color.White);

        var lineStatus = services.Localization.T("loading.status");
        var lineName = SpriteFontText.Sanitize(loadingTargetLabel);
        var m1 = services.Font.MeasureString(lineStatus);
        var m2 = services.Font.MeasureString(lineName);
        const float gap = 10f;
        const float bottomMargin = 28f;
        var blockH = m1.Y + gap + m2.Y;
        var y1 = vh - bottomMargin - blockH;
        var y2 = y1 + m1.Y + gap;
        var x1 = (vw - m1.X) / 2f;
        var x2 = (vw - m2.X) / 2f;

        DrawOutlineString(spriteBatch, services.Font, lineStatus, new Vector2(x1, y1), UITheme.TextPrimary);
        DrawOutlineString(spriteBatch, services.Font, lineName, new Vector2(x2, y2), UITheme.Accent);
        spriteBatch.End();
    }

    private static void DrawOutlineString(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color fill)
    {
        var shadow = new Color(0, 0, 0, 220);
        for (var ox = -1; ox <= 1; ox++)
        {
            for (var oy = -1; oy <= 1; oy++)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                spriteBatch.DrawString(font, text, position + new Vector2(ox, oy), shadow);
            }
        }

        spriteBatch.DrawString(font, text, position, fill);
    }
}
