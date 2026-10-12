using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PhisicsGame.Core;
using PhisicsGame.Gameplay;
using PhisicsGame.Core.Scenes;
using PhisicsGame.Core.Text;
using PhisicsGame.Core.Graphics;
using PhisicsGame.Core.UI;
using PhisicsGame.Core.Interop;
using PhisicsGame.Tasks;

namespace PhisicsGame.Gameplay.Scenes;

/// <summary>Мир раздела: один терминал открывает WPF «Задачи» только по задачам этой темы.</summary>
public sealed class TopicWorldScene(
    SceneManager sceneManager,
    GameServices services,
    string topicId,
    string topicTitle) : IScene
{
    private readonly TileMap _map = new();
    private Matrix _worldMatrix;
    private Vector2 _playerPosition = new(520, 420);
    private Vector2 _tasksPortalCenter;
    private float _runCycle;
    private bool _facingRight = true;
    private bool _professorHintVisible;
    private string _tasksAppToast = string.Empty;
    private float _tasksAppToastTimer;
    private readonly List<SubtopicTerminal> _subtopicTerminals = [];
    private int _nearSubtopicTerminalIndex = -1;

    private const float TasksPortalInteractRadius = 92f;

    public void OnEnter()
    {
        _runCycle = 0f;
        _tasksAppToast = string.Empty;
        _tasksAppToastTimer = 0f;
        _professorHintVisible = false;
        _subtopicTerminals.Clear();

        var bounds = _map.GetBounds();
        _tasksPortalCenter = new Vector2(bounds.Width * 0.5f - 20f, bounds.Height * 0.5f - 20f);
        BuildSubtopicTerminals();
        _worldMatrix = Camera2D.GetStaticWorldMatrix(bounds.Width, bounds.Height);
    }

    public void OnExit()
    {
    }

    public void Update(GameTime gameTime)
    {
        if (services.Input.IsJustPressed(Keys.Escape))
        {
            sceneManager.ChangeScene(new PortalHubScene(sceneManager, services));
            return;
        }

        if (_tasksAppToastTimer > 0f)
        {
            _tasksAppToastTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_tasksAppToastTimer <= 0f)
                _tasksAppToast = string.Empty;
        }

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

        var next = _playerPosition + input * 240f * dt;
        if (_map.IsWalkable(next + PlayerSprite.FootAnchor))
            _playerPosition = next;

        var nearCatalog = IsNearTasksPortal();
        if (!nearCatalog)
            _professorHintVisible = false;
        else if (services.Input.IsJustPressed(Keys.Tab))
            _professorHintVisible = !_professorHintVisible;

        _nearSubtopicTerminalIndex = FindNearestSubtopicTerminal();

        if (services.Input.IsJustPressed(Keys.E))
        {
            if (_nearSubtopicTerminalIndex >= 0)
            {
                TryLaunchSubtopicCatalog(_subtopicTerminals[_nearSubtopicTerminalIndex]);
                return;
            }

            if (!nearCatalog)
                return;

            if (!WpfBridgeLauncher.TryLaunchTasksTerminal(topicId, out var err))
            {
                _tasksAppToast = $"Не удалось открыть задачи: {err}";
                _tasksAppToastTimer = 5f;
                return;
            }

            _tasksAppToast = "Открыто приложение «Задачи» для этого раздела.";
            _tasksAppToastTimer = 4f;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var bounds = _map.GetBounds();
        spriteBatch.Begin(transformMatrix: _worldMatrix);
        _map.Draw(spriteBatch, services.Pixel);
        spriteBatch.Draw(services.Pixel, new Rectangle(0, 0, bounds.Width, bounds.Height), new Color(8, 18, 36, 110));

        var t = (float)gameTime.TotalGameTime.TotalSeconds;
        DrawSubtopicTerminals(spriteBatch, t);
        var topicTasks = services.TaskRepository.GetByTopic(topicId);
        var catalogSolved = topicTasks.Count > 0 && topicTasks.All(d => services.SaveData.CompletedTaskIds.Contains(d.Id));
        var glow = new Rectangle((int)_tasksPortalCenter.X - 6, (int)_tasksPortalCenter.Y - 6, 52, 52);
        var pulse = 0.35f + (float)Math.Sin(t * 3f) * 0.14f;
        spriteBatch.Draw(services.Pixel, glow, (catalogSolved ? UITheme.Success : UITheme.Accent) * pulse);
        var icon = new Rectangle((int)_tasksPortalCenter.X, (int)_tasksPortalCenter.Y, 40, 40);
        spriteBatch.Draw(services.TaskIconTexture, icon, Color.White);
        spriteBatch.Draw(services.Pixel, new Rectangle(icon.X + 10, icon.Y + 10, 20, 20), catalogSolved ? UITheme.Success : UITheme.Accent);
        var centerLabel = $"Все задачи по {topicTitle.ToUpper(CultureInfo.CurrentCulture)}";
        var centerLabelScale = 0.54f;
        var centerLabelWidth = services.Font.MeasureString(SpriteFontText.Sanitize(centerLabel)).X * centerLabelScale;
        spriteBatch.DrawString(
            services.Font,
            SpriteFontText.Sanitize(centerLabel),
            new Vector2(_tasksPortalCenter.X + 20f - centerLabelWidth * 0.5f, _tasksPortalCenter.Y - 24f),
            UITheme.TextPrimary,
            0f,
            Vector2.Zero,
            centerLabelScale,
            SpriteEffects.None,
            0f);

        spriteBatch.End();

        spriteBatch.Begin(transformMatrix: _worldMatrix, samplerState: SamplerState.PointClamp);
        DrawPlayer(spriteBatch);
        spriteBatch.End();

        var nearCatalog = IsNearTasksPortal();

        spriteBatch.Begin();
        var controlsText = "WASD - передвижение, E - открыть, ESC - выйти";
        var textWidth = services.Font.MeasureString(SpriteFontText.Sanitize(controlsText)).X * 0.80f;
        var panelWidth = (int)MathF.Ceiling(textWidth) + 48;
        var controlsPanel = new Rectangle(16, 16, panelWidth, 56);
        UIPrimitives.DrawRoundedPanel(spriteBatch, services.Pixel, controlsPanel, 12, new Color(26, 34, 56, 220));
        spriteBatch.DrawString(services.Font, SpriteFontText.Sanitize(controlsText), new Vector2(32, 34), UITheme.TextPrimary, 0f, Vector2.Zero, 0.80f, SpriteEffects.None, 0f);

        UIPrimitives.DrawVignette(spriteBatch, services.Pixel, new Rectangle(0, 0, 1280, 720));
        spriteBatch.End();

        if (_professorHintVisible && nearCatalog && services.InteractionHintTexture.Width > 1)
        {
            InteractionHintPanel.Draw(
                spriteBatch,
                services.InteractionHintTexture,
                services.Font,
                services.Localization.T("hint.panel.topicCatalog", topicTitle),
                1f);
        }
    }

    private bool IsNearTasksPortal()
    {
        var player = _playerPosition + PlayerSprite.FootAnchor;
        var portalMid = _tasksPortalCenter + new Vector2(20, 20);
        return Vector2.Distance(player, portalMid) <= TasksPortalInteractRadius;
    }

    private void DrawPlayer(SpriteBatch spriteBatch)
    {
        var moving = _runCycle > 0.01f;
        PlayerSprite.Draw(spriteBatch, services.PlayerTexture, services.Pixel, _playerPosition, _runCycle, moving, _facingRight);
    }

    private void BuildSubtopicTerminals()
    {
        var source = TaskWpfSetupCatalog.GetTeacherLabSetupsForTopic(topicId);
        if (source.Count == 0)
            return;

        var bounds = _map.GetBounds();
        var roomLeft = 90f;
        var roomRight = bounds.Width - 90f;
        var rowTopY = 170f;
        var rowBottomY = MathF.Min(bounds.Height - 170f, _tasksPortalCenter.Y + 170f);
        var count = source.Count;
        var usableWidth = MathF.Max(1f, roomRight - roomLeft);
        var stepX = count > 1 ? usableWidth / (count - 1) : 0f;
        for (var i = 0; i < source.Count; i++)
        {
            var (setupId, label) = source[i];
            var x = roomLeft + i * stepX;
            var y = (i % 2 == 0) ? rowTopY : rowBottomY;
            _subtopicTerminals.Add(new SubtopicTerminal(setupId, label, new Vector2(x - 20f, y - 20f)));
        }
    }

    private int FindNearestSubtopicTerminal()
    {
        if (_subtopicTerminals.Count == 0)
            return -1;

        var player = _playerPosition + PlayerSprite.FootAnchor;
        var best = -1;
        var bestDist = float.MaxValue;
        for (var i = 0; i < _subtopicTerminals.Count; i++)
        {
            var portalMid = _subtopicTerminals[i].Center + new Vector2(20, 20);
            var dist = Vector2.Distance(player, portalMid);
            if (dist <= SubtopicTerminal.InteractRadius && dist < bestDist)
            {
                best = i;
                bestDist = dist;
            }
        }

        return best;
    }

    private void TryLaunchSubtopicCatalog(SubtopicTerminal terminal)
    {
        if (!WpfBridgeLauncher.TryLaunchTasksTerminal(topicId, terminal.SetupId, out var err))
        {
            _tasksAppToast = $"Не удалось открыть каталог «{terminal.Label}»: {err}";
            _tasksAppToastTimer = 5f;
            return;
        }

        _tasksAppToast = $"Открыт каталог задач: {terminal.Label}.";
        _tasksAppToastTimer = 4f;
    }

    private void DrawSubtopicTerminals(SpriteBatch spriteBatch, float time)
    {
        for (var i = 0; i < _subtopicTerminals.Count; i++)
        {
            var terminal = _subtopicTerminals[i];
            var isNear = i == _nearSubtopicTerminalIndex;
            var pulse = 0.25f + (float)Math.Sin((time + i) * 2.8f) * 0.10f;
            var glowColor = isNear ? UITheme.Success : UITheme.Accent;

            var glow = new Rectangle((int)terminal.Center.X - 6, (int)terminal.Center.Y - 6, 52, 52);
            spriteBatch.Draw(services.Pixel, glow, glowColor * pulse);

            var icon = new Rectangle((int)terminal.Center.X, (int)terminal.Center.Y, 40, 40);
            spriteBatch.Draw(services.TaskIconTexture, icon, Color.White);
            spriteBatch.Draw(
                services.Pixel,
                new Rectangle(icon.X + 10, icon.Y + 10, 20, 20),
                isNear ? UITheme.Success : UITheme.Accent);

            var labelScale = 0.52f;
            var maxLabelWidth = 136f;
            var (line1, line2) = SplitLabelToTwoLines(terminal.Label, maxLabelWidth, labelScale);
            var centerX = terminal.Center.X + 20f;
            var line1Width = services.Font.MeasureString(SpriteFontText.Sanitize(line1)).X * labelScale;
            var line2Width = services.Font.MeasureString(SpriteFontText.Sanitize(line2)).X * labelScale;
            var line1Pos = new Vector2(centerX - line1Width * 0.5f, terminal.Center.Y - 24);
            spriteBatch.DrawString(
                services.Font,
                SpriteFontText.Sanitize(line1),
                line1Pos,
                UITheme.TextPrimary,
                0f,
                Vector2.Zero,
                labelScale,
                SpriteEffects.None,
                0f);
            if (!string.IsNullOrWhiteSpace(line2))
            {
                var line2Pos = new Vector2(centerX - line2Width * 0.5f, terminal.Center.Y - 10);
                spriteBatch.DrawString(
                    services.Font,
                    SpriteFontText.Sanitize(line2),
                    line2Pos,
                    UITheme.TextPrimary,
                    0f,
                    Vector2.Zero,
                    labelScale,
                    SpriteEffects.None,
                    0f);
            }
        }
    }

    private (string line1, string line2) SplitLabelToTwoLines(string text, float maxWidth, float scale)
    {
        var clean = text.Trim();
        if (clean.Length == 0)
            return (string.Empty, string.Empty);

        if (services.Font.MeasureString(SpriteFontText.Sanitize(clean)).X * scale <= maxWidth)
            return (clean, string.Empty);

        var words = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
            return (clean, string.Empty);

        var bestLeft = clean;
        var bestRight = string.Empty;
        var bestScore = float.MaxValue;
        for (var i = 1; i < words.Length; i++)
        {
            var left = string.Join(' ', words.Take(i));
            var right = string.Join(' ', words.Skip(i));
            var lw = services.Font.MeasureString(SpriteFontText.Sanitize(left)).X * scale;
            var rw = services.Font.MeasureString(SpriteFontText.Sanitize(right)).X * scale;
            var overflow = MathF.Max(0f, lw - maxWidth) + MathF.Max(0f, rw - maxWidth);
            var balance = MathF.Abs(lw - rw) * 0.2f;
            var score = overflow * 10f + balance;
            if (score < bestScore)
            {
                bestScore = score;
                bestLeft = left;
                bestRight = right;
            }
        }

        return (bestLeft, bestRight);
    }

    private sealed record SubtopicTerminal(string SetupId, string Label, Vector2 Center)
    {
        public const float InteractRadius = 70f;
    }
}
