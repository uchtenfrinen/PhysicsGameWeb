using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PhisicsGame.Core;
using PhisicsGame.Core.Graphics;
using PhisicsGame.Core.Interop;
using PhisicsGame.Core.Scenes;
using PhisicsGame.Core.Text;
using PhisicsGame.Core.UI;
using PhisicsGame.Core.UI.Elements;
using PhisicsGame.Core.UI.Layout;
using PhisicsGame.Tasks;

namespace PhisicsGame.Gameplay.Scenes;

public sealed class TaskScene(
    SceneManager sceneManager,
    GameServices services,
    PhysicsTask task,
    Action<PhysicsTask> onSuccess,
    Func<IScene> backSceneFactory,
    int taskIndex = 0,
    int taskTotal = 0,
    string? topicId = null) : IScene
{
    private enum TaskSidebarTab
    {
        Theory = 0,
        Tasks = 1,
        Board = 2
    }

    private enum InputFeedbackKind
    {
        None,
        Success,
        Fail
    }

    private readonly TaskRuntimeState _state = new();
    private readonly UIRoot _ui = new();
    private readonly Label _hint = new();
    private readonly List<Button> _choiceButtons = [];
    private string _resultKey = "task.controls";
    private float _fade = 0f;
    private float _exitDelay = 0f;

    private string _descriptionText = string.Empty;
    private string _formulaText = string.Empty;

    private readonly int _taskIndex = taskIndex;
    private readonly int _taskTotal = taskTotal;
    private readonly string? _topicId = topicId;

    private Rectangle _contentInner;
    private Rectangle _headerRect;
    private Rectangle _taskCardRect;
    private Rectangle _labPanelRect;
    private Rectangle _formulaGlassRect;
    private Rectangle _numericInputRect;
    private Rectangle _checkButtonRect;
    private Rectangle _choiceLayoutRect;

    private float _caretBlinkPhase;
    private float _inputFeedbackTimer;
    private InputFeedbackKind _inputFeedback = InputFeedbackKind.None;
    private bool _checkButtonHover;

    private string _labWpfSetupId = string.Empty;
    private bool _labHasWpf;
    private bool _labOpenHover;
    private Rectangle _labSchematicRect;
    private Rectangle _labOpenButtonRect;
    private float _labErrorTimer;
    private string _labLaunchError = string.Empty;
    private Texture2D? _taskImageTexture;

    private TaskSidebarTab _selectedTab = TaskSidebarTab.Tasks;
    private SpriteBatch? _boardBatch;
    private RenderTarget2D? _boardTarget;
    private Vector2? _lastBoardPoint;
    private static readonly string[] SidebarKeys =
    {
        "task.sidebar.theory",
        "task.sidebar.tasks",
        "task.sidebar.board"
    };

    private const int SidebarWidth = 236;
    private const int SidebarItemTop = 96;
    private const int SidebarItemStep = 48;
    private const int SidebarItemHeight = 44;
    private static readonly Rectangle MainOuter = new(248, 28, 1020, 664);

    private static Rectangle BoardCanvasRect => new(254, 100, 1008, 560);

    public void OnEnter()
    {
        var r = BoardCanvasRect;
        _boardTarget = new RenderTarget2D(
            services.GraphicsDevice,
            r.Width,
            r.Height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
        _boardBatch = new SpriteBatch(services.GraphicsDevice);
        var prevRtOnEnter = services.GraphicsDevice.GetRenderTargets();
        services.GraphicsDevice.SetRenderTarget(_boardTarget);
        services.GraphicsDevice.Clear(new Color(14, 16, 24));
        RestoreRenderTargetAndViewport(prevRtOnEnter);
        _lastBoardPoint = null;

        ComputeTaskLayout();
        ResolveLabWpfSetup();
        TryLoadTaskImage();
        BuildLayout();
        ApplyTabVisibility();
        UpdateTitle();
    }

    private void ResolveLabWpfSetup()
    {
        _labLaunchError = string.Empty;
        _labErrorTimer = 0f;
        var pref = task.GetLabWpfSetupPreference();
        if (string.Equals(pref, "none", StringComparison.OrdinalIgnoreCase))
        {
            _labHasWpf = false;
            _labWpfSetupId = string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(pref) && TaskWpfSetupCatalog.IsKnownLabSetupId(pref))
        {
            _labHasWpf = true;
            _labWpfSetupId = pref.Trim();
            return;
        }

        _labHasWpf = TaskWpfSetupCatalog.TryGetWpfSetup(task.Id, _topicId, task.Kind, out var id);
        _labWpfSetupId = _labHasWpf ? id : string.Empty;
    }

    private void TryLoadTaskImage()
    {
        _taskImageTexture?.Dispose();
        _taskImageTexture = null;
        if (string.IsNullOrWhiteSpace(task.ImagePath))
        {
            return;
        }

        var path = task.ImagePath;
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path.Replace('/', Path.DirectorySeparatorChar));
        }

        if (!File.Exists(path))
        {
            return;
        }

        _taskImageTexture = TaskPhotoTextureLoader.TryLoad(services.GraphicsDevice, path);
    }

    private void ComputeTaskLayout()
    {
        var inner = new Rectangle(MainOuter.X + 10, MainOuter.Y + 10, MainOuter.Width - 20, MainOuter.Height - 20);
        _contentInner = inner;
        _headerRect = new Rectangle(inner.X, inner.Y, inner.Width, 44);
        _taskCardRect = new Rectangle(inner.X, inner.Y + 52, 552, 168);
        _labPanelRect = new Rectangle(inner.Right - 400, inner.Y + 52, 392, inner.Height - 108);
        var labPad = 14;
        var labInnerW = _labPanelRect.Width - labPad * 2;
        _labOpenButtonRect = new Rectangle(_labPanelRect.X + labPad, _labPanelRect.Bottom - 48, labInnerW, 38);
        _labSchematicRect = new Rectangle(
            _labPanelRect.X + labPad,
            _labPanelRect.Y + 66,
            labInnerW,
            Math.Max(48, _labOpenButtonRect.Y - (_labPanelRect.Y + 66) - 10));
        _formulaGlassRect = new Rectangle(inner.X, _taskCardRect.Bottom + 14, 552, 72);
        var inputY = _formulaGlassRect.Bottom + 12;
        _numericInputRect = new Rectangle(inner.X, inputY, 364, 48);
        _checkButtonRect = new Rectangle(inner.X + 376, inputY, 176, 48);
        var choiceTop = inputY + 56;
        _choiceLayoutRect = new Rectangle(inner.X, choiceTop, 552, Math.Max(120, inner.Bottom - choiceTop - 72));
    }

    public void OnExit()
    {
        _boardBatch?.Dispose();
        _boardBatch = null;
        _boardTarget?.Dispose();
        _boardTarget = null;
        _taskImageTexture?.Dispose();
        _taskImageTexture = null;
    }

    public void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fade = MathHelper.Clamp(_fade + dt * 2.2f, 0f, 1f);
        _caretBlinkPhase += dt * 2.6f;
        if (_inputFeedbackTimer > 0f)
        {
            _inputFeedbackTimer -= dt;
            if (_inputFeedbackTimer <= 0f)
            {
                _inputFeedback = InputFeedbackKind.None;
            }
        }

        if (_labErrorTimer > 0f)
        {
            _labErrorTimer -= dt;
            if (_labErrorTimer <= 0f)
            {
                _labLaunchError = string.Empty;
            }
        }

        _ui.Update(gameTime, services.Input);

        if (services.Input.IsJustPressed(Keys.Escape))
        {
            sceneManager.ChangeScene(backSceneFactory());
            return;
        }

        if (_exitDelay > 0f)
        {
            _exitDelay -= dt;
            if (_exitDelay <= 0f)
            {
                sceneManager.ChangeScene(backSceneFactory());
            }

            return;
        }

        HandleSidebarClicks();
        HandleSidebarHotkeys();

        if (_selectedTab == TaskSidebarTab.Tasks)
        {
            UpdateInputByTaskType(gameTime);
            UpdateCheckButtonHover();
            UpdateLabOpenHover();
            if (services.Input.IsLeftMouseJustPressed() && _checkButtonHover)
            {
                TrySubmitAnswer();
            }

            if (services.Input.IsLeftMouseJustPressed() && _labOpenHover && _labHasWpf && !services.HostedInWpfLab)
            {
                TryOpenLabWpfInstall();
            }

            if (services.Input.IsJustPressed(Keys.Enter))
            {
                TrySubmitAnswer();
            }
        }

        UpdateHint();
        UpdateTitle();
    }

    private void UpdateCheckButtonHover()
    {
        if (_selectedTab != TaskSidebarTab.Tasks)
        {
            _checkButtonHover = false;
            return;
        }

        var m = services.Input.GetMouseVirtual(services.GraphicsDevice);
        _checkButtonHover = _checkButtonRect.Contains(m);
    }

    private void UpdateLabOpenHover()
    {
        if (_selectedTab != TaskSidebarTab.Tasks || !_labHasWpf || services.HostedInWpfLab)
        {
            _labOpenHover = false;
            return;
        }

        var m = services.Input.GetMouseVirtual(services.GraphicsDevice);
        _labOpenHover = _labOpenButtonRect.Contains(m);
    }

    private void TryOpenLabWpfInstall()
    {
        if (!WpfBridgeLauncher.TryLaunchSetup(_labWpfSetupId, out var err))
        {
            _labLaunchError = err;
            _labErrorTimer = 4f;
            return;
        }

        _labLaunchError = string.Empty;
        _labErrorTimer = 0f;
    }

    private void TrySubmitAnswer()
    {
        if (task.Validate(_state))
        {
            _resultKey = "task.success";
            ApplyResultButtonState(true);
            onSuccess(task);
            _inputFeedback = InputFeedbackKind.Success;
            _inputFeedbackTimer = 0.45f;
            _exitDelay = 0.55f;
            return;
        }

        _resultKey = "task.fail";
        ApplyResultButtonState(false);
        _inputFeedback = InputFeedbackKind.Fail;
        _inputFeedbackTimer = 0.5f;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Рисование на RenderTarget только здесь (до SpriteBatch основной сцены), с актуальным Mouse.GetState().
        // Рендер в Update часто даёт пустую текстуру из‑за несинхронного viewport/GPU на DesktopGL.
        if (_selectedTab == TaskSidebarTab.Board)
        {
            RenderBoardDrawingToRenderTarget();
        }

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        DrawTaskChrome(spriteBatch);

        _hint.Alpha = _fade;
        foreach (var button in _choiceButtons)
        {
            button.Alpha = _fade;
        }

        if (_selectedTab == TaskSidebarTab.Tasks)
        {
            DrawTasksTabForeground(spriteBatch);
        }

        _ui.Draw(spriteBatch, services.Pixel, services.Font);

        if (_selectedTab == TaskSidebarTab.Board)
        {
            DrawBoardView(spriteBatch);
        }
        else if (_selectedTab == TaskSidebarTab.Theory)
        {
            DrawTheoryContent(spriteBatch);
        }

        UIPrimitives.DrawVignette(spriteBatch, services.Pixel, new Rectangle(0, 0, 1280, 720));
        spriteBatch.End();
    }

    private void HandleSidebarClicks()
    {
        if (!services.Input.IsLeftMouseJustPressed())
        {
            return;
        }

        var m = services.Input.GetMouseVirtual(services.GraphicsDevice);
        for (var i = 0; i < SidebarKeys.Length; i++)
        {
            if (!GetSidebarItemRect(i).Contains(m))
            {
                continue;
            }

            _selectedTab = (TaskSidebarTab)i;
            ApplyTabVisibility();
            _lastBoardPoint = null;
            return;
        }
    }

    private void HandleSidebarHotkeys()
    {
        if (services.Input.IsJustPressed(Keys.D1) || services.Input.IsJustPressed(Keys.NumPad1))
        {
            _selectedTab = TaskSidebarTab.Theory;
            ApplyTabVisibility();
            return;
        }

        if (services.Input.IsJustPressed(Keys.D2) || services.Input.IsJustPressed(Keys.NumPad2))
        {
            _selectedTab = TaskSidebarTab.Tasks;
            ApplyTabVisibility();
            return;
        }

        if (services.Input.IsJustPressed(Keys.D3) || services.Input.IsJustPressed(Keys.NumPad3))
        {
            _selectedTab = TaskSidebarTab.Board;
            ApplyTabVisibility();
        }
    }

    private static Rectangle GetSidebarItemRect(int index)
    {
        return new Rectangle(14, SidebarItemTop + index * SidebarItemStep, SidebarWidth - 28, SidebarItemHeight);
    }

    /// <summary>
    /// Мышь в координатах сцены 1280×720 (та же letterbox-математика, что и <see cref="InputState.GetMouseVirtual"/>).
    /// </summary>
    private Point GetMousePointForBoard(MouseState m)
    {
        var bw = VirtualResolution.PhysicalBackBufferWidth > 0
            ? VirtualResolution.PhysicalBackBufferWidth
            : services.GraphicsDevice.PresentationParameters.BackBufferWidth;
        var bh = VirtualResolution.PhysicalBackBufferHeight > 0
            ? VirtualResolution.PhysicalBackBufferHeight
            : services.GraphicsDevice.PresentationParameters.BackBufferHeight;
        if (bw <= 0 || bh <= 0)
        {
            return new Point(Math.Clamp(m.X, 0, 1279), Math.Clamp(m.Y, 0, 719));
        }

        VirtualResolution.GetLetterboxRectangle(bw, bh, out var vx, out var vy, out var vw, out var vh);
        if (vw <= 0 || vh <= 0)
        {
            return new Point(Math.Clamp(m.X, 0, 1279), Math.Clamp(m.Y, 0, 719));
        }

        var x = (m.X - vx) * (1280f / vw);
        var y = (m.Y - vy) * (720f / vh);
        return new Point(
            (int)Math.Clamp(Math.Round(x), 0, 1279),
            (int)Math.Clamp(Math.Round(y), 0, 719));
    }

    private void RenderBoardDrawingToRenderTarget()
    {
        if (_boardTarget is null || _boardBatch is null)
        {
            return;
        }

        if (services.Input.IsJustPressed(Keys.C))
        {
            var gd = services.GraphicsDevice;
            var prevClear = gd.GetRenderTargets();
            gd.SetRenderTarget(_boardTarget);
            gd.Clear(new Color(14, 16, 24));
            RestoreRenderTargetAndViewport(prevClear);
            _lastBoardPoint = null;
            return;
        }

        var canvas = BoardCanvasRect;
        // Один снимок SDL: иначе координата из конца кадра, а ЛКМ из InputState с начала кадра — линия не рисуется.
        var mouseState = Mouse.GetState();
        var mouse = GetMousePointForBoard(mouseState);
        var mouseDown = mouseState.LeftButton == ButtonState.Pressed;

        if (!canvas.Contains(mouse))
        {
            if (!mouseDown)
            {
                _lastBoardPoint = null;
            }

            return;
        }

        var local = new Vector2(mouse.X - canvas.X, mouse.Y - canvas.Y);
        local.X = MathHelper.Clamp(local.X, 0, _boardTarget.Width - 1);
        local.Y = MathHelper.Clamp(local.Y, 0, _boardTarget.Height - 1);

        if (!mouseDown)
        {
            _lastBoardPoint = null;
            return;
        }

        var gd2 = services.GraphicsDevice;
        var prevDraw = gd2.GetRenderTargets();
        gd2.SetRenderTarget(_boardTarget);
        // AlphaBlend + непрозрачный цвет: стабильнее на разных драйверах, чем Opaque на RT.
        _boardBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        var stroke = Color.FromNonPremultiplied(UITheme.Accent.R, UITheme.Accent.G, UITheme.Accent.B, 255);
        if (_lastBoardPoint.HasValue)
        {
            DrawBoardStrokeSegment(_boardBatch, _lastBoardPoint.Value, local, 3.2f, stroke);
        }
        else
        {
            // Первый кадр штриха / один клик — кисть (линия нулевой длины в DrawLine не рисуется).
            const int brush = 4;
            var bx = (int)local.X - brush / 2;
            var by = (int)local.Y - brush / 2;
        }

        _boardBatch.End();
        RestoreRenderTargetAndViewport(prevDraw);
        _lastBoardPoint = local;
    }

    /// <summary>
    /// После работы с отдельным RT нельзя вызывать SetRenderTarget(null): во время Draw сцена рендерится в _sceneTarget Game1, иначе UI уходит в backbuffer и даёт чёрный экран.
    /// </summary>
    private void RestoreRenderTargetAndViewport(RenderTargetBinding[] previous)
    {
        var gd = services.GraphicsDevice;
        gd.SetRenderTargets(previous);
        if (previous.Length == 0 || previous[0].RenderTarget == null)
        {
            var pp = gd.PresentationParameters;
            gd.Viewport = new Viewport(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
        }
        else if (previous[0].RenderTarget is RenderTarget2D rt)
        {
            gd.Viewport = new Viewport(0, 0, rt.Width, rt.Height);
        }
        else
        {
            var pp = gd.PresentationParameters;
            gd.Viewport = new Viewport(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
        }
    }

    /// <summary>Несколько отрезков между кадрами — иначе при быстром движении остаются дыры.</summary>
    private void DrawBoardStrokeSegment(SpriteBatch sb, Vector2 from, Vector2 to, float thickness, Color color)
    {
        var len = (to - from).Length();
        if (len < 0.001f)
        {
            return;
        }

        var steps = Math.Max(1, (int)Math.Ceiling(len / 3f));
        for (var i = 0; i < steps; i++)
        {
            var t0 = i / (float)steps;
            var t1 = (i + 1) / (float)steps;
            var a = Vector2.Lerp(from, to, t0);
            var b = Vector2.Lerp(from, to, t1);
            UIPrimitives.DrawLine(sb, services.Pixel, a, b, thickness, color);
        }
    }

    private void DrawBoardView(SpriteBatch spriteBatch)
    {
        if (_boardTarget is null)
        {
            return;
        }

        var r = BoardCanvasRect;
        UIPrimitives.DrawRoundedPanel(spriteBatch, services.Pixel, r, 12, new Color(6, 10, 18, 240) * _fade);
        spriteBatch.Draw(_boardTarget, r, Color.White * _fade);
        spriteBatch.Draw(services.Pixel, new Rectangle(r.X, r.Y, r.Width, 2), UITheme.Accent * 0.7f * _fade);
        spriteBatch.Draw(services.Pixel, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), UITheme.Accent * 0.45f * _fade);
        spriteBatch.Draw(services.Pixel, new Rectangle(r.X, r.Y, 2, r.Height), UITheme.Accent * 0.45f * _fade);
        spriteBatch.Draw(services.Pixel, new Rectangle(r.Right - 2, r.Y, 2, r.Height), UITheme.Accent * 0.45f * _fade);
    }

    private void DrawTheoryContent(SpriteBatch spriteBatch)
    {
        var f = _fade;
        var font = services.Font;
        var pinkGlow = new Color(255, 120, 200);
        var pinkMain = new Color(255, 85, 170);
        UIPrimitives.DrawNeonTitle(
            spriteBatch,
            font,
            services.Localization.T("task.theory.output"),
            new Vector2(300, 118),
            pinkGlow,
            pinkMain,
            1.05f,
            f);

        var desc = services.Localization.T("task.theory.integralDesc");
        spriteBatch.DrawString(
            font,
            SpriteFontText.Sanitize(desc),
            new Vector2(300, 168),
            new Color(185, 225, 255) * f,
            0f,
            Vector2.Zero,
            0.92f,
            SpriteEffects.None,
            0f);

        UIPrimitives.DrawDefiniteIntegral(
            spriteBatch,
            font,
            services.Localization.T("task.theory.integralLo"),
            services.Localization.T("task.theory.integralHi"),
            services.Localization.T("task.theory.integralArg"),
            new Vector2(300, 218),
            UITheme.Accent * f,
            new Color(140, 210, 255) * f,
            new Color(160, 235, 255) * f,
            f);
    }

    private void ApplyTabVisibility()
    {
        var task = _selectedTab == TaskSidebarTab.Tasks;
        _hint.Visible = true;
        foreach (var b in _choiceButtons)
        {
            b.Visible = task;
        }
    }

    private void DrawTaskChrome(SpriteBatch spriteBatch)
    {
        var f = _fade;
        var px = services.Pixel;

        spriteBatch.Draw(px, new Rectangle(0, 0, 1280, 720), new Color(10, 14, 20) * f);

        spriteBatch.Draw(px, new Rectangle(0, 0, SidebarWidth, 720), new Color(8, 12, 24) * f);
        spriteBatch.Draw(px, new Rectangle(SidebarWidth - 2, 0, 2, 720), UITheme.Accent * 0.18f * f);
        spriteBatch.DrawString(services.Font, "*", new Vector2(22, 18), UITheme.Accent * 0.85f * f, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);

        for (var i = 0; i < SidebarKeys.Length; i++)
        {
            var rect = GetSidebarItemRect(i);
            var selected = (int)_selectedTab == i;
            if (selected)
            {
                spriteBatch.Draw(px, new Rectangle(rect.X, rect.Y, 5, rect.Height), UITheme.Accent * 0.95f * f);
                UIPrimitives.DrawRoundedPanel(spriteBatch, px, rect, 10, new Color(22, 38, 62, 245) * f);
                spriteBatch.Draw(px, new Rectangle(rect.X + 5, rect.Y, rect.Width - 5, 2), UITheme.Accent * 0.95f * f);
                spriteBatch.Draw(px, new Rectangle(rect.X + 5, rect.Bottom - 2, rect.Width - 5, 2), UITheme.Accent * 0.65f * f);
            }

            var label = services.Localization.T(SidebarKeys[i]);
            var col = selected ? new Color(210, 245, 255) : UITheme.TextSecondary;
            spriteBatch.DrawString(services.Font, SpriteFontText.Sanitize(label), new Vector2(34, rect.Y + 12), col * f, 0f, Vector2.Zero, 0.84f, SpriteEffects.None, 0f);
        }

        spriteBatch.DrawString(services.Font, "*", new Vector2(22, 688), UITheme.Accent * 0.5f * f);

        UIPrimitives.DrawMetalFrame(spriteBatch, px, MainOuter, new Color(78, 84, 94) * f, new Color(16, 20, 34) * f);
        UIPrimitives.DrawCornerScrews(spriteBatch, px, MainOuter, new Color(95, 100, 108) * f);

        if (_selectedTab != TaskSidebarTab.Tasks)
        {
            var titleText = services.Localization.T(SidebarKeys[(int)_selectedTab]);
            UIPrimitives.DrawNeonTitle(
                spriteBatch,
                services.Font,
                titleText,
                new Vector2(290, 54),
                new Color(0, 220, 255),
                UITheme.Accent,
                _selectedTab == TaskSidebarTab.Board ? 1.05f : 1.12f,
                f);
            DrawBottomBar(spriteBatch, px, f);
            return;
        }

        UIPrimitives.DrawBlueprintGrid(spriteBatch, px, _contentInner, UITheme.Accent * 0.4f, f);
        UIPrimitives.DrawRoundedPanel(spriteBatch, px, _taskCardRect, 12, new Color(12, 18, 32, 228) * f);
        spriteBatch.Draw(px, new Rectangle(_taskCardRect.X, _taskCardRect.Y, _taskCardRect.Width, 2), UITheme.Accent * 0.35f * f);

        UIPrimitives.DrawRoundedPanel(spriteBatch, px, _labPanelRect, 14, new Color(10, 16, 28, 235) * f);
        spriteBatch.Draw(px, new Rectangle(_labPanelRect.X, _labPanelRect.Y, _labPanelRect.Width, 2), UITheme.Accent * 0.4f * f);

        spriteBatch.Draw(px, _headerRect, new Color(6, 12, 22, 210) * f);
        spriteBatch.Draw(px, new Rectangle(_headerRect.X, _headerRect.Y, _headerRect.Width, 2), UITheme.Accent * 0.55f * f);

        UIPrimitives.DrawGlassPanel(
            spriteBatch,
            px,
            _formulaGlassRect,
            UITheme.Accent,
            new Color(120, 200, 255, 28),
            f);

        DrawBottomBar(spriteBatch, px, f);
    }

    private void DrawBottomBar(SpriteBatch spriteBatch, Texture2D px, float f)
    {
        var bar = new Rectangle(248, 672, 1020, 40);
        spriteBatch.Draw(px, bar, new Color(6, 10, 18, 240) * f);
        spriteBatch.Draw(px, new Rectangle(bar.X, bar.Y, bar.Width, 1), UITheme.Accent * 0.25f * f);
    }

    private void DrawTasksTabForeground(SpriteBatch spriteBatch)
    {
        var f = _fade;
        var font = services.Font;
        var taskTitle = services.Localization.T(task.TitleKey);

        var iconRect = new Rectangle(_headerRect.X + 10, _headerRect.Y + 4, 36, 36);
        DrawTopicIcon(spriteBatch, iconRect, f);

        UIPrimitives.DrawNeonTitle(
            spriteBatch,
            font,
            taskTitle,
            new Vector2(_headerRect.X + 52, _headerRect.Y + 4),
            new Color(0, 220, 255),
            UITheme.Accent,
            1.22f,
            f);

        if (_taskTotal > 0 && _taskIndex > 0)
        {
            var prog = services.Localization.T("task.progress", _taskIndex, _taskTotal);
            var safe = SpriteFontText.Sanitize(prog);
            var scale = 0.78f;
            var psz = font.MeasureString(safe) * scale;
            var pill = new Rectangle((int)(_headerRect.Right - psz.X - 28), _headerRect.Y + 8, (int)psz.X + 20, 28);
            UIPrimitives.DrawRoundedPanel(spriteBatch, services.Pixel, pill, 8, new Color(18, 36, 58, 220) * f);
            spriteBatch.DrawString(font, safe, new Vector2(pill.X + 10, pill.Y + 6), UITheme.Accent * f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        var textPad = new Rectangle(_taskCardRect.X + 14, _taskCardRect.Y + 10, _taskCardRect.Width - 28, _taskCardRect.Height - 20);
        TaskInstructionText.DrawWrappedWithVariables(
            spriteBatch,
            font,
            _descriptionText,
            textPad,
            0.98f,
            UITheme.TextPrimary,
            new Color(255, 165, 95),
            f);

        var formulaSafe = SpriteFontText.Sanitize(_formulaText);
        var formulaPos = new Vector2(_formulaGlassRect.X + 16, _formulaGlassRect.Y + 20);
        spriteBatch.DrawString(
            font,
            formulaSafe,
            formulaPos,
            new Color(175, 245, 255) * f,
            0f,
            Vector2.Zero,
            1.08f,
            SpriteEffects.None,
            0f);

        DrawLabPanel(spriteBatch, f);
        DrawCheckButton(spriteBatch, f);

        switch (task.Kind)
        {
            case TaskKind.Numeric:
                DrawNumericTask(spriteBatch);
                break;
            case TaskKind.Simulation:
                DrawSimulationTask(spriteBatch);
                break;
            case TaskKind.Choice:
                DrawChoiceFeedbackOutline(spriteBatch);
                break;
        }
    }

    private void DrawChoiceFeedbackOutline(SpriteBatch spriteBatch)
    {
        if (_inputFeedbackTimer <= 0f)
        {
            return;
        }

        var px = services.Pixel;
        var r = _choiceLayoutRect;
        var a = MathHelper.Clamp(_inputFeedbackTimer / 0.5f, 0f, 1f) * 0.85f;
        var c = _inputFeedback == InputFeedbackKind.Success ? UITheme.Success : UITheme.Error;
        var thick = 3;
        spriteBatch.Draw(px, new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, thick), c * a);
        spriteBatch.Draw(px, new Rectangle(r.X - 2, r.Bottom - 1, r.Width + 4, thick), c * a);
        spriteBatch.Draw(px, new Rectangle(r.X - 2, r.Y - 2, thick, r.Height + 4), c * a);
        spriteBatch.Draw(px, new Rectangle(r.Right - 1, r.Y - 2, thick, r.Height + 4), c * a);
    }

    private void DrawTopicIcon(SpriteBatch spriteBatch, Rectangle r, float f)
    {
        var px = services.Pixel;
        UIPrimitives.DrawRoundedPanel(spriteBatch, px, r, 8, new Color(12, 22, 40, 240) * f);
        spriteBatch.Draw(px, new Rectangle(r.X, r.Y, r.Width, 2), UITheme.Accent * 0.5f * f);
        var topic = _topicId?.ToLowerInvariant() ?? string.Empty;
        var c = UITheme.Accent * f;
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        switch (topic)
        {
            case "optics":
                spriteBatch.Draw(px, new Rectangle(cx - 10, cy - 2, 20, 4), c);
                spriteBatch.Draw(px, new Rectangle(cx - 6, cy - 8, 3, 6), c * 0.85f);
                spriteBatch.Draw(px, new Rectangle(cx + 3, cy - 8, 3, 6), c * 0.85f);
                break;
            case "mechanics":
                UIPrimitives.DrawLine(spriteBatch, px, new Vector2(r.X + 8, cy), new Vector2(r.Right - 8, cy), 3f, c);
                spriteBatch.Draw(px, new Rectangle(cx - 2, cy - 8, 4, 8), c * 0.9f);
                break;
            case "waves":
                for (var i = 0; i < 3; i++)
                {
                    var y = r.Y + 10 + i * 6;
                    UIPrimitives.DrawLine(spriteBatch, px, new Vector2(r.X + 8, y), new Vector2(r.Right - 8, y + 4), 2f, c * (0.7f + i * 0.1f));
                }

                break;
            case "electrostatics":
                spriteBatch.Draw(px, new Rectangle(cx - 2, r.Y + 8, 4, 14), c);
                UIPrimitives.DrawLine(spriteBatch, px, new Vector2(cx - 8, r.Y + 10), new Vector2(cx, r.Y + 18), 2.5f, c);
                UIPrimitives.DrawLine(spriteBatch, px, new Vector2(cx, r.Y + 18), new Vector2(cx + 8, r.Y + 10), 2.5f, c);
                break;
            default:
                spriteBatch.Draw(px, new Rectangle(cx - 6, cy - 6, 12, 12), c * 0.35f);
                break;
        }
    }

    private void DrawLabPanel(SpriteBatch spriteBatch, float f)
    {
        var font = services.Font;
        var r = _labPanelRect;
        var title = services.Localization.T("task.lab.title");
        var hint = services.Localization.T("task.lab.hint");
        spriteBatch.DrawString(font, SpriteFontText.Sanitize(title), new Vector2(r.X + 16, r.Y + 12), UITheme.Accent * f, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, SpriteFontText.Sanitize(hint), new Vector2(r.X + 16, r.Y + 38), UITheme.TextSecondary * f, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);

        UIPrimitives.DrawRoundedPanel(spriteBatch, services.Pixel, _labSchematicRect, 10, new Color(6, 12, 22, 200) * f);
        DrawLabSchematic(spriteBatch, _labSchematicRect, f);

        if (services.HostedInWpfLab)
        {
            var emb = services.Localization.T("task.lab.embeddedWpf");
            spriteBatch.DrawString(font, SpriteFontText.Sanitize(emb), new Vector2(_labOpenButtonRect.X, _labOpenButtonRect.Y - 28), UITheme.TextSecondary * f, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
        }
        else if (_labHasWpf)
        {
            var setupKey = TaskWpfSetupCatalog.GetLocalizationKeyForSetup(_labWpfSetupId);
            var setupName = services.Localization.T(setupKey);
            spriteBatch.DrawString(
                font,
                SpriteFontText.Sanitize(setupName),
                new Vector2(_labOpenButtonRect.X, _labOpenButtonRect.Y - 26),
                UITheme.TextPrimary * f,
                0f,
                Vector2.Zero,
                0.74f,
                SpriteEffects.None,
                0f);
            DrawLabOpenInstallButton(spriteBatch, f);
        }
        else
        {
            var no = services.Localization.T("task.lab.noWpf");
            spriteBatch.DrawString(font, SpriteFontText.Sanitize(no), new Vector2(_labOpenButtonRect.X, _labOpenButtonRect.Y - 6), UITheme.TextSecondary * f, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
        }

        if (_labErrorTimer > 0f && !string.IsNullOrEmpty(_labLaunchError))
        {
            var err = services.Localization.T("task.lab.wpfFailed", _labLaunchError);
            spriteBatch.DrawString(font, SpriteFontText.Sanitize(err), new Vector2(_labOpenButtonRect.X, _labOpenButtonRect.Bottom + 4), UITheme.Error * f, 0f, Vector2.Zero, 0.62f, SpriteEffects.None, 0f);
        }
    }

    private void DrawLabOpenInstallButton(SpriteBatch spriteBatch, float f)
    {
        var font = services.Font;
        var r = _labOpenButtonRect;
        var bg = _labOpenHover ? new Color(38, 72, 96, 245) : new Color(22, 48, 68, 235);
        UIPrimitives.DrawRoundedPanel(spriteBatch, services.Pixel, r, 10, bg * f);
        var border = _labOpenHover ? UITheme.Accent : UITheme.Accent * 0.5f;
        spriteBatch.Draw(services.Pixel, new Rectangle(r.X, r.Y, r.Width, 2), border * f);
        spriteBatch.Draw(services.Pixel, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), border * 0.65f * f);
        var label = services.Localization.T("task.lab.openWpf");
        var safe = SpriteFontText.Sanitize(label);
        var sz = font.MeasureString(safe);
        var pos = new Vector2(r.Center.X - sz.X * 0.45f, r.Center.Y - sz.Y * 0.45f);
        spriteBatch.DrawString(font, safe, pos, UITheme.TextPrimary * f, 0f, Vector2.Zero, 0.82f, SpriteEffects.None, 0f);
    }

    private void DrawLabSchematic(SpriteBatch spriteBatch, Rectangle area, float f)
    {
        var px = services.Pixel;
        var topic = _topicId?.ToLowerInvariant() ?? string.Empty;
        var c = UITheme.Accent * 0.75f * f;
        var cx = area.X + area.Width / 2;
        var cy = area.Y + area.Height / 2;

        if (_taskImageTexture is not null && _taskImageTexture.Width > 0 && _taskImageTexture.Height > 0)
        {
            var scale = Math.Min(area.Width / (float)_taskImageTexture.Width, area.Height / (float)_taskImageTexture.Height);
            var w = Math.Max(1, (int)Math.Round(_taskImageTexture.Width * scale));
            var h = Math.Max(1, (int)Math.Round(_taskImageTexture.Height * scale));
            var dest = new Rectangle(area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
            spriteBatch.Draw(_taskImageTexture, dest, Color.White * (0.9f * f));
            spriteBatch.Draw(px, new Rectangle(area.X, area.Y, area.Width, 1), UITheme.Accent * 0.32f * f);
            spriteBatch.Draw(px, new Rectangle(area.X, area.Bottom - 1, area.Width, 1), UITheme.Accent * 0.24f * f);
            return;
        }

        switch (task.Kind)
        {
            case TaskKind.Simulation:
                UIPrimitives.DrawLine(spriteBatch, px, new Vector2(area.X + 20, cy), new Vector2(area.Right - 20, cy), 2f, UITheme.TextSecondary * 0.4f * f);
                spriteBatch.Draw(px, new Rectangle(cx - 16, cy - 10, 32, 20), c);
                break;
            case TaskKind.Choice:
                for (var i = 0; i < 3; i++)
                {
                    var x = area.X + 24 + i * 56;
                    UIPrimitives.DrawRoundedPanel(spriteBatch, px, new Rectangle(x, cy - 14, 44, 28), 6, new Color(20, 32, 52, 200) * f);
                }

                break;
            default:
                if (topic == "optics")
                {
                    spriteBatch.Draw(px, new Rectangle(cx - 24, cy - 4, 48, 8), c);
                    UIPrimitives.DrawLine(spriteBatch, px, new Vector2(cx - 14, cy - 14), new Vector2(cx - 10, cy - 4), 2f, c);
                    UIPrimitives.DrawLine(spriteBatch, px, new Vector2(cx + 10, cy - 14), new Vector2(cx + 14, cy - 4), 2f, c);
                }
                else
                {
                    UIPrimitives.DrawLine(spriteBatch, px, new Vector2(area.X + 24, cy + 20), new Vector2(area.Right - 24, cy - 24), 2f, c);
                    UIPrimitives.DrawLine(spriteBatch, px, new Vector2(area.X + 24, cy - 10), new Vector2(area.Right - 24, cy + 14), 2f, c * 0.65f);
                }

                break;
        }
    }

    private void DrawCheckButton(SpriteBatch spriteBatch, float f)
    {
        var font = services.Font;
        var r = _checkButtonRect;
        var bg = _checkButtonHover ? new Color(42, 62, 98, 245) : new Color(28, 38, 62, 235);
        UIPrimitives.DrawRoundedPanel(spriteBatch, services.Pixel, r, 10, bg * f);
        var border = _checkButtonHover ? UITheme.Accent : UITheme.Accent * 0.55f;
        spriteBatch.Draw(services.Pixel, new Rectangle(r.X, r.Y, r.Width, 2), border * f);
        spriteBatch.Draw(services.Pixel, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), border * 0.65f * f);
        var label = services.Localization.T("task.check");
        var safe = SpriteFontText.Sanitize(label);
        var sz = font.MeasureString(safe);
        var pos = new Vector2(r.Center.X - sz.X * 0.46f, r.Center.Y - sz.Y * 0.46f);
        spriteBatch.DrawString(font, safe, pos, UITheme.TextPrimary * f, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
    }

    private void BuildLayout()
    {
        _ui.Clear();
        _choiceButtons.Clear();

        if (task.Kind == TaskKind.Choice)
        {
            var layout = new VerticalLayout { Bounds = _choiceLayoutRect, Spacing = 10 };
            for (var i = 0; i < task.Choices.Length; i++)
            {
                var btn = new Button { Text = services.Localization.T(task.Choices[i]) };
                _choiceButtons.Add(btn);
                _ui.Add(btn);
            }

            var items = new List<UIElement>();
            foreach (var button in _choiceButtons)
            {
                items.Add(button);
            }

            layout.Arrange(items, 44);
        }

        _hint.Bounds = new Rectangle(248, 668, 1020, 36);
        _hint.Color = UITheme.TextSecondary;
        _hint.Scale = 0.86f;
        _ui.Add(_hint);
    }

    private void UpdateInputByTaskType(GameTime gameTime)
    {
        switch (task.Kind)
        {
            case TaskKind.Numeric:
                UpdateNumericInput();
                break;
            case TaskKind.Choice:
                UpdateChoiceInput(gameTime);
                break;
            case TaskKind.Simulation:
                UpdateSimulationInput();
                break;
        }
    }

    private void UpdateTitle()
    {
        _descriptionText = services.Localization.T(task.DescriptionKey);
        _formulaText = services.Localization.T(task.FormulaKey);
        services.SetWindowTitle($"Task | {services.Localization.T(task.TitleKey)} | {services.Localization.T(_resultKey)}");
    }

    private void UpdateNumericInput()
    {
        var ctrl = services.Input.IsPressed(Keys.LeftControl) || services.Input.IsPressed(Keys.RightControl);
        if (ctrl && services.Input.IsJustPressed(Keys.V))
        {
            AppendNumericFromClipboard();
        }

        HandleDigit(Keys.D0, "0");
        HandleDigit(Keys.D1, "1");
        HandleDigit(Keys.D2, "2");
        HandleDigit(Keys.D3, "3");
        HandleDigit(Keys.D4, "4");
        HandleDigit(Keys.D5, "5");
        HandleDigit(Keys.D6, "6");
        HandleDigit(Keys.D7, "7");
        HandleDigit(Keys.D8, "8");
        HandleDigit(Keys.D9, "9");
        HandleDigit(Keys.NumPad0, "0");
        HandleDigit(Keys.NumPad1, "1");
        HandleDigit(Keys.NumPad2, "2");
        HandleDigit(Keys.NumPad3, "3");
        HandleDigit(Keys.NumPad4, "4");
        HandleDigit(Keys.NumPad5, "5");
        HandleDigit(Keys.NumPad6, "6");
        HandleDigit(Keys.NumPad7, "7");
        HandleDigit(Keys.NumPad8, "8");
        HandleDigit(Keys.NumPad9, "9");

        if ((services.Input.IsJustPressed(Keys.OemMinus) || services.Input.IsJustPressed(Keys.Subtract)) && _state.NumericInput.Length == 0)
        {
            _state.NumericInput = "-";
        }

        if (services.Input.IsJustPressed(Keys.OemPeriod) || services.Input.IsJustPressed(Keys.Decimal) || services.Input.IsJustPressed(Keys.OemComma))
        {
            if (!_state.NumericInput.Contains('.') && !_state.NumericInput.Contains(','))
            {
                if (_state.NumericInput.Length == 0 || _state.NumericInput == "-")
                {
                    _state.NumericInput += "0";
                }

                _state.NumericInput += ".";
            }
        }

        if (services.Input.IsJustPressed(Keys.Back) && _state.NumericInput.Length > 0)
        {
            _state.NumericInput = _state.NumericInput[..^1];
        }

        if (ctrl && services.Input.IsJustPressed(Keys.Back))
        {
            _state.NumericInput = string.Empty;
        }
    }

    private void HandleDigit(Keys key, string digit)
    {
        if (services.Input.IsJustPressed(key) && _state.NumericInput.Length < 14)
        {
            _state.NumericInput += digit;
        }
    }

    private void AppendNumericFromClipboard()
    {
        var raw = TryReadClipboardText();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var trimmed = raw.Trim();
        var sb = new StringBuilder();
        for (var i = 0; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];
            if ((ch == '-' || ch == '+') && i == 0)
            {
                sb.Append(ch == '+' ? string.Empty : "-");
                continue;
            }

            if (char.IsDigit(ch))
            {
                sb.Append(ch);
                continue;
            }

            if ((ch == '.' || ch == ',') && !sb.ToString().Contains('.') && !sb.ToString().Contains(','))
            {
                if (sb.Length == 0 || sb.ToString() == "-")
                {
                    sb.Append('0');
                }

                sb.Append('.');
            }
        }

        var parsed = sb.ToString();
        if (!string.IsNullOrWhiteSpace(parsed))
        {
            _state.NumericInput = parsed.Length > 14 ? parsed[..14] : parsed;
        }
    }

    private static string TryReadClipboardText()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command Get-Clipboard -Raw",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
            {
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(300);
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void UpdateChoiceInput(GameTime gameTime)
    {
        if (services.Input.IsJustPressed(Keys.Up))
        {
            _state.SelectedChoiceIndex = Math.Max(0, _state.SelectedChoiceIndex - 1);
        }

        if (services.Input.IsJustPressed(Keys.Down))
        {
            _state.SelectedChoiceIndex = Math.Min(task.Choices.Length - 1, _state.SelectedChoiceIndex + 1);
        }

        var mousePoint = services.Input.GetMouseVirtual(services.GraphicsDevice);
        for (var i = 0; i < _choiceButtons.Count; i++)
        {
            var btn = _choiceButtons[i];
            var isMouseOver = btn.Bounds.Contains(mousePoint);
            if (isMouseOver)
            {
                _state.SelectedChoiceIndex = i;
            }

            btn.VisualState = isMouseOver
                ? UIButtonVisualState.Hover
                : i == _state.SelectedChoiceIndex ? UIButtonVisualState.Selected : UIButtonVisualState.Normal;
            btn.Update(gameTime, services.Input);
        }
    }

    private void UpdateSimulationInput()
    {
        if (services.Input.IsJustPressed(Keys.Left))
        {
            _state.SimulationValue = Math.Max(task.SimulationMin, _state.SimulationValue - task.SimulationStep);
        }

        if (services.Input.IsJustPressed(Keys.Right))
        {
            _state.SimulationValue = Math.Min(task.SimulationMax, _state.SimulationValue + task.SimulationStep);
        }
    }

    private void DrawNumericTask(SpriteBatch spriteBatch)
    {
        var inputRect = _numericInputRect;
        var px = services.Pixel;
        var f = _fade;
        UIPrimitives.DrawRoundedPanel(spriteBatch, px, inputRect, 10, new Color(4, 10, 14, 252) * f);
        spriteBatch.Draw(px, new Rectangle(inputRect.X, inputRect.Y, inputRect.Width, 2), new Color(0, 200, 170) * 0.65f * f);

        if (_inputFeedbackTimer > 0f)
        {
            var a = MathHelper.Clamp(_inputFeedbackTimer / 0.45f, 0f, 1f);
            var fc = _inputFeedback == InputFeedbackKind.Success ? UITheme.Success : UITheme.Error;
            var thick = 3;
            spriteBatch.Draw(px, new Rectangle(inputRect.X - 2, inputRect.Y - 2, inputRect.Width + 4, thick), fc * a);
            spriteBatch.Draw(px, new Rectangle(inputRect.X - 2, inputRect.Bottom - 1, inputRect.Width + 4, thick), fc * a);
            spriteBatch.Draw(px, new Rectangle(inputRect.X - 2, inputRect.Y - 2, thick, inputRect.Height + 4), fc * a);
            spriteBatch.Draw(px, new Rectangle(inputRect.Right - 1, inputRect.Y - 2, thick, inputRect.Height + 4), fc * a);
        }

        var text = _state.NumericInput;
        var scale = 1.12f;
        var textX = inputRect.X + 16f;
        var textY = inputRect.Y + 13f;
        if (!string.IsNullOrEmpty(text))
        {
            var safe = SpriteFontText.Sanitize(text);
            spriteBatch.DrawString(
                services.Font,
                safe,
                new Vector2(textX, textY),
                new Color(130, 255, 230) * f,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
            textX += services.Font.MeasureString(safe).X * scale;
        }
        else
        {
            spriteBatch.DrawString(
                services.Font,
                SpriteFontText.Sanitize("Введите число..."),
                new Vector2(textX, textY),
                new Color(128, 150, 176) * f,
                0f,
                Vector2.Zero,
                0.86f,
                SpriteEffects.None,
                0f);
        }

        var blink = (MathF.Sin(_caretBlinkPhase * MathF.PI) * 0.5f + 0.5f) > 0.35f;
        if (blink)
        {
            spriteBatch.Draw(px, new Rectangle((int)textX + 2, inputRect.Y + 10, 2, 22), new Color(0, 255, 210) * f);
        }

        const string label = "Ответ:";
        spriteBatch.DrawString(services.Font, SpriteFontText.Sanitize(label), new Vector2(inputRect.X + 2, inputRect.Y - 22), UITheme.TextSecondary * f, 0f, Vector2.Zero, 0.82f, SpriteEffects.None, 0f);
    }

    private void DrawSimulationTask(SpriteBatch spriteBatch)
    {
        var barRect = new Rectangle(_numericInputRect.X, _numericInputRect.Y + 8, 552, 24);
        var f = _fade;
        var px = services.Pixel;
        spriteBatch.Draw(px, barRect, new Color(28, 34, 48) * f);
        var fill = (_state.SimulationValue - task.SimulationMin) / (task.SimulationMax - task.SimulationMin);
        var fillWidth = (int)(barRect.Width * fill);
        spriteBatch.Draw(px, new Rectangle(barRect.X, barRect.Y, fillWidth, barRect.Height), UITheme.Accent * f);

        if (_inputFeedbackTimer > 0f)
        {
            var a = MathHelper.Clamp(_inputFeedbackTimer / 0.45f, 0f, 1f);
            var fc = _inputFeedback == InputFeedbackKind.Success ? UITheme.Success : UITheme.Error;
            const int thick = 3;
            spriteBatch.Draw(px, new Rectangle(barRect.X - 2, barRect.Y - 2, barRect.Width + 4, thick), fc * a);
            spriteBatch.Draw(px, new Rectangle(barRect.X - 2, barRect.Bottom - 1, barRect.Width + 4, thick), fc * a);
            spriteBatch.Draw(px, new Rectangle(barRect.X - 2, barRect.Y - 2, thick, barRect.Height + 4), fc * a);
            spriteBatch.Draw(px, new Rectangle(barRect.Right - 1, barRect.Y - 2, thick, barRect.Height + 4), fc * a);
        }

        const string simLabel = "Значение:";
        spriteBatch.DrawString(
            services.Font,
            SpriteFontText.Sanitize($"{simLabel} {_state.SimulationValue:0.0}"),
            new Vector2(_numericInputRect.X, _numericInputRect.Y - 22),
            UITheme.TextPrimary * f);
    }

    private void UpdateHint()
    {
        if (_selectedTab == TaskSidebarTab.Board)
        {
            _hint.Text = services.Localization.T("task.board.hint");
            _hint.Color = UITheme.TextSecondary;
            return;
        }

        if (_selectedTab != TaskSidebarTab.Tasks)
        {
            _hint.Text = "Esc - назад";
            _hint.Color = UITheme.TextSecondary;
            return;
        }

        _hint.Text = task.Kind switch
        {
            TaskKind.Numeric => "Введите ответ, Ctrl+V, Enter - проверить | 1/2/3 - вкладки | Esc - назад",
            TaskKind.Choice => "Стрелки, Enter - проверить | 1/2/3 - вкладки | Esc - назад",
            TaskKind.Simulation => "Влево/вправо, Enter - проверить | 1/2/3 - вкладки | Esc - назад",
            _ => string.Empty
        };
        _hint.Color = _resultKey switch
        {
            "task.success" => UITheme.Success,
            "task.fail" => UITheme.Error,
            _ => UITheme.TextSecondary
        };
    }

    private void ApplyResultButtonState(bool success)
    {
        if (task.Kind != TaskKind.Choice || _choiceButtons.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _choiceButtons.Count; i++)
        {
            if (i == _state.SelectedChoiceIndex)
            {
                _choiceButtons[i].VisualState = success ? UIButtonVisualState.Correct : UIButtonVisualState.Wrong;
            }
            else
            {
                _choiceButtons[i].VisualState = UIButtonVisualState.Normal;
            }
        }
    }
}
