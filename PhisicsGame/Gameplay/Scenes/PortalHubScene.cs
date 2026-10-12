using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PhisicsGame.Core;
using PhisicsGame.Core.Scenes;
using PhisicsGame.Core.Graphics;
using PhisicsGame.Core.UI;
using PhisicsGame.Gameplay;
using PhisicsGame.Gameplay.Entities;
using PhisicsGame.Gameplay.Entities.Components;

namespace PhisicsGame.Gameplay.Scenes;

public sealed class PortalHubScene(SceneManager sceneManager, GameServices services) : IScene
{
    private static readonly Point HubPortalSize = new(292, 214);
    private const float HubPortalInteractionRadius = 168f;

    private sealed record PortalDef(string TopicId, string Title, Vector2 Position, Point Size, float InteractionRadius, Color Color, bool UseInstallationTexture = false, int InstallationId = 0);

    private readonly TileMap _map = new();
    private Matrix _worldMatrix;
    private readonly Entity _player = new();
    private readonly List<PortalDef> _portals = [];
    private bool _facingRight = true;
    private float _runCycle;
    /// <summary>Первое E у портала — подсказка, второе — вход в мир темы (один терминал задач WPF).</summary>
    private bool _portalProfessorStepDone;

    public void OnEnter()
    {
        _player.AddComponent(new TransformComponent(new Vector2(560, 430)));
        _player.AddComponent(new PlayerControllerComponent(240f));
        _runCycle = 0f;
        _portalProfessorStepDone = false;
        _portals.Clear();
        _portals.Add(new PortalDef("mechanics", "Механика", new Vector2(46, 36), HubPortalSize, HubPortalInteractionRadius, new Color(76, 201, 240), true, 1));
        _portals.Add(new PortalDef("electrostatics", "Электростатика", new Vector2(406, 88), HubPortalSize, HubPortalInteractionRadius, new Color(143, 80, 255), true, 2));
        _portals.Add(new PortalDef("optics", "Оптика", new Vector2(176, 304), HubPortalSize, HubPortalInteractionRadius, new Color(52, 170, 255), true, 3));
        _portals.Add(new PortalDef("hydro", "Гидростатика", new Vector2(710, 250), HubPortalSize, HubPortalInteractionRadius, new Color(76, 201, 240), true, 4));
        _portals.Add(new PortalDef("thermodynamics", "Термодинамика", new Vector2(804, 40), HubPortalSize, HubPortalInteractionRadius, new Color(255, 148, 82), true, 6));
        _portals.Add(new PortalDef("waves", "Колебания", new Vector2(568, 440), HubPortalSize, HubPortalInteractionRadius, new Color(112, 196, 255), true, 5));
        var b = _map.GetBounds();
        _worldMatrix = Camera2D.GetStaticWorldMatrix(b.Width, b.Height);
        services.SetWindowTitle("Physics Adventure | " + services.Localization.T("menu.topicPortals"));
    }

    public void OnExit()
    {
    }

    public void Update(GameTime gameTime)
    {
        if (services.Input.IsJustPressed(Keys.Escape))
        {
            sceneManager.ChangeScene(new MainMenuScene(sceneManager, services));
            return;
        }

        var transform = _player.Get<TransformComponent>();
        var speed = _player.Get<PlayerControllerComponent>().Speed;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var input = Vector2.Zero;
        if (services.Input.IsPressed(Keys.W)) input.Y -= 1;
        if (services.Input.IsPressed(Keys.S)) input.Y += 1;
        if (services.Input.IsPressed(Keys.A)) input.X -= 1;
        if (services.Input.IsPressed(Keys.D)) input.X += 1;
        if (input.LengthSquared() > 0f)
        {
            input.Normalize();
            _facingRight = input.X >= 0f;
            _runCycle = PlayerSprite.AdvanceWalkCycle(_runCycle, dt, true);
        }
        else
        {
            _runCycle = 0f;
        }

        var next = transform.Position + input * speed * dt;
        if (_map.IsWalkable(next + PlayerSprite.FootAnchor))
            transform.Position = next;

        var nearPortal = GetNearestPortal();
        if (nearPortal is null)
            _portalProfessorStepDone = false;

        if (services.Input.IsJustPressed(Keys.E))
        {
            var portal = nearPortal;
            if (portal is null)
                return;

            if (!_portalProfessorStepDone)
            {
                _portalProfessorStepDone = true;
                return;
            }

            sceneManager.ChangeScene(new WorldLoadingScene(
                sceneManager,
                services,
                () => new TopicWorldScene(sceneManager, services, portal.TopicId, portal.Title),
                portal.Title));
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var playerPos = _player.Get<TransformComponent>().Position;
        var mapBounds = _map.GetBounds();
        spriteBatch.Begin(transformMatrix: _worldMatrix, samplerState: SamplerState.PointClamp);
        _map.Draw(spriteBatch, services.Pixel);
        spriteBatch.Draw(services.Pixel, new Rectangle(0, 0, mapBounds.Width, mapBounds.Height), new Color(8, 18, 36, 120));

        foreach (var portal in _portals)
        {
            var body = new Rectangle((int)portal.Position.X, (int)portal.Position.Y, portal.Size.X, portal.Size.Y);
            if (portal.UseInstallationTexture && services.PortalInstallation01Texture.Width > 1)
            {
                var installationTexture = portal.InstallationId switch
                {
                    2 => services.PortalInstallation02Texture,
                    3 => services.PortalInstallation03Texture,
                    4 => services.PortalInstallation04Texture,
                    5 => services.PortalInstallation05Texture,
                    6 => services.PortalInstallation06Texture,
                    _ => services.PortalInstallation01Texture,
                };
                if (installationTexture.Width <= 1)
                    installationTexture = services.PortalInstallation01Texture;

                DrawTextureFitted(spriteBatch, installationTexture, body, Color.White);
            }
            else
            {
                spriteBatch.Draw(services.TaskIconTexture, body, Color.White);
                spriteBatch.Draw(services.Pixel, new Rectangle(body.X + body.Width / 4, body.Y + body.Height / 4, body.Width / 2, body.Height / 2), portal.Color);
            }
        }

        spriteBatch.End();

        spriteBatch.Begin(transformMatrix: _worldMatrix, samplerState: SamplerState.PointClamp);
        DrawPlayer(spriteBatch, playerPos);
        spriteBatch.End();

        spriteBatch.Begin();
        var controlsText = "WASD - передвижение, E - открыть, ESC - выйти";
        var textWidth = services.Font.MeasureString(controlsText).X * 0.80f;
        var panelWidth = (int)MathF.Ceiling(textWidth) + 48;
        UIPrimitives.DrawRoundedPanel(spriteBatch, services.Pixel, new Rectangle(16, 16, panelWidth, 56), 12, new Color(26, 34, 56, 220));
        spriteBatch.DrawString(services.Font, controlsText, new Vector2(32, 34), UITheme.TextPrimary, 0f, Vector2.Zero, 0.80f, SpriteEffects.None, 0f);

        UIPrimitives.DrawVignette(spriteBatch, services.Pixel, new Rectangle(0, 0, 1280, 720));
        spriteBatch.End();

        var near = GetNearestPortal();

        if (_portalProfessorStepDone && near is not null)
        {
            var popup = services.GetPortalHubPopupTexture(near.TopicId);
            if (popup.Width > 1)
                PortalPopupOverlay.Draw(spriteBatch, popup, 1f);
        }
    }

    private static void DrawTextureFitted(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, Color color)
    {
        if (texture.Width <= 0 || texture.Height <= 0)
            return;

        var scale = Math.Min(destination.Width / (float)texture.Width, destination.Height / (float)texture.Height);
        var dw = Math.Max(1, (int)Math.Round(texture.Width * scale));
        var dh = Math.Max(1, (int)Math.Round(texture.Height * scale));
        var dx = destination.X + (destination.Width - dw) / 2;
        var dy = destination.Y + (destination.Height - dh) / 2;
        spriteBatch.Draw(texture, new Rectangle(dx, dy, dw, dh), color);
    }

    private PortalDef? GetNearestPortal()
    {
        var playerCenter = _player.Get<TransformComponent>().Position + PlayerSprite.FootAnchor;
        foreach (var portal in _portals)
        {
            var center = portal.Position + new Vector2(portal.Size.X * 0.5f, portal.Size.Y * 0.5f);
            if (Vector2.Distance(playerCenter, center) <= portal.InteractionRadius)
                return portal;
        }

        return null;
    }

    private void DrawPlayer(SpriteBatch spriteBatch, Vector2 playerPos)
    {
        var moving = _runCycle > 0.01f;
        PlayerSprite.Draw(spriteBatch, services.PlayerTexture, services.Pixel, playerPos, _runCycle, moving, _facingRight);
    }
}
