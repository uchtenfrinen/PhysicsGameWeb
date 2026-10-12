using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PhisicsGame.Core;
using PhisicsGame.Core.Interop;
using PhisicsGame.Core.Scenes;
using PhisicsGame.Core.Text;

namespace PhisicsGame.Gameplay.Scenes;

public sealed class MainMenuScene(SceneManager sceneManager, GameServices services) : IScene
{
    private int _selectedIndex;
    private string _subtitle = "Исследуй. Решай. Побеждай.";

    public void OnEnter()
    {
        SetTitle();
    }

    public void OnExit()
    {
    }

    public void Update(GameTime gameTime)
    {
        var menuCount = GetMenuLabels().Length;
        if (services.Input.IsJustPressed(Keys.Up))
        {
            _selectedIndex = (_selectedIndex - 1 + menuCount) % menuCount;
        }

        if (services.Input.IsJustPressed(Keys.Down))
        {
            _selectedIndex = (_selectedIndex + 1) % menuCount;
        }

        if (services.Input.IsJustPressed(Keys.Enter))
        {
            switch (_selectedIndex)
            {
                case 0:
                    sceneManager.ChangeScene(new PortalHubScene(sceneManager, services));
                    break;
                case 1:
                    if (!WpfBridgeLauncher.TryLaunch(out var labErr))
                    {
                        _subtitle = services.Localization.CurrentLanguage == "ru"
                            ? $"Не удалось открыть лабораторию: {labErr}"
                            : $"Could not open laboratory: {labErr}";
                    }
                    else
                    {
                        _subtitle = services.Localization.CurrentLanguage == "ru"
                            ? "Запущена лаборатория."
                            : "Laboratory started.";
                    }

                    break;
                case 2:
                    if (!WpfBridgeLauncher.TryLaunchTeacherPanel(out var teacherErr))
                    {
                        _subtitle = $"Не удалось открыть модуль преподавателя: {teacherErr}";
                    }
                    else
                    {
                        _subtitle = "Запущено окно преподавателя (ForTeacher / ForCheater) с вашим tasks.json.";
                    }

                    break;
                case 3:
                    Environment.Exit(0);
                    break;
            }
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var time = (float)gameTime.TotalGameTime.TotalSeconds;
        var glow = 0.55f + (float)Math.Sin(time * 2.4f) * 0.22f;

        // Фон: Assets/Images/menu_bg.png на весь экран; панель справа — на картинке учёный слева.
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        if (services.MenuBackgroundTexture.Width > 1)
        {
            spriteBatch.Draw(services.MenuBackgroundTexture, new Rectangle(0, 0, 1280, 720), Color.White);
            spriteBatch.Draw(services.Pixel, new Rectangle(0, 0, 1280, 720), new Color(6, 10, 18, 85));
        }
        else
        {
            spriteBatch.Draw(services.Pixel, new Rectangle(0, 0, 1280, 720), new Color(10, 14, 22));
            spriteBatch.Draw(services.Pixel, new Rectangle(0, 0, 1280, 720), new Color(8, 12, 24, 120));
        }

        var panelRect = new Rectangle(480, 95, 720, 485);
        var panelGlow = new Rectangle(panelRect.X - 4, panelRect.Y - 4, panelRect.Width + 8, panelRect.Height + 8);
        spriteBatch.Draw(services.Pixel, panelGlow, new Color(76, 201, 240) * (0.12f + glow * 0.1f));
        spriteBatch.Draw(services.Pixel, panelRect, new Color(18, 28, 50, 210));

        spriteBatch.Draw(services.Pixel, new Rectangle(panelRect.X + 24, panelRect.Y + 24, panelRect.Width - 48, 2), new Color(76, 201, 240) * (0.5f + glow * 0.4f));

        var titleX = panelRect.X + 48;
        spriteBatch.DrawString(services.Font, SpriteFontText.Sanitize("PHYSICS ADVENTURE"), new Vector2(titleX, panelRect.Y + 42), new Color(197, 238, 255));
        spriteBatch.DrawString(services.Font, SpriteFontText.Sanitize(_subtitle), new Vector2(titleX, panelRect.Y + 86), new Color(176, 192, 222));

        var menuLabels = GetMenuLabels();
        for (var i = 0; i < menuLabels.Length; i++)
        {
            var y = panelRect.Y + 168 + i * 62;
            var isSelected = i == _selectedIndex;
            var itemRect = new Rectangle(panelRect.X + 55, y, panelRect.Width - 110, 48);
            var bgColor = isSelected ? new Color(32, 65, 102, 220) : new Color(24, 40, 66, 185);
            var borderColor = isSelected ? new Color(76, 201, 240) : new Color(89, 128, 185);
            spriteBatch.Draw(services.Pixel, itemRect, bgColor);
            spriteBatch.Draw(services.Pixel, new Rectangle(itemRect.X, itemRect.Y, itemRect.Width, 2), borderColor);
            spriteBatch.Draw(services.Pixel, new Rectangle(itemRect.X, itemRect.Bottom - 2, itemRect.Width, 2), borderColor);
            spriteBatch.DrawString(services.Font, SpriteFontText.Sanitize((isSelected ? ">  " : "   ") + menuLabels[i]), new Vector2(itemRect.X + 16, itemRect.Y + 10), new Color(218, 236, 255));
        }

        spriteBatch.End();
    }

    private void SetTitle()
    {
        services.SetWindowTitle(
            "Physics Adventure | "
            + services.Localization.T("menu.start")
            + " | "
            + services.Localization.T("menu.topicPortals"));
    }

    private string[] GetMenuLabels() =>
    [
        services.Localization.T("menu.topicPortals"),
        services.Localization.CurrentLanguage == "ru" ? "Лаборатория" : "Laboratory",
        services.Localization.CurrentLanguage == "ru" ? "Панель преподавателя" : "Teacher panel",
        services.Localization.T("menu.exit")
    ];
}
    