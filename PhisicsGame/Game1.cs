using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PhisicsGame.Core;
using PhisicsGame.Core.Dialog;
using PhisicsGame.Core.Graphics;
using PhisicsGame.Core.Input;
using PhisicsGame.Core.Interop;
using PhisicsGame.Core.Localization;
using PhisicsGame.Core.Save;
using PhisicsGame.Core.Scenes;
using PhisicsGame.Core.Tasks;
using PhisicsGame.Core.UI;
using PhisicsGame.Gameplay.Scenes;

namespace PhisicsGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _playerTexture = null!;
    private Texture2D _menuCharacterTexture = null!;
    private Texture2D _menuBackgroundTexture = null!;
    private Texture2D _portalInstallation01Texture = null!;
    private Texture2D _portalInstallation02Texture = null!;
    private Texture2D _portalInstallation03Texture = null!;
    private Texture2D _portalInstallation04Texture = null!;
    private Texture2D _portalInstallation05Texture = null!;
    private Texture2D _portalInstallation06Texture = null!;
    private Texture2D _taskIconTexture = null!;
    private Texture2D _interactionHintTexture = null!;
    private Texture2D _portalPopupMechanicsTexture = null!;
    private Texture2D _portalPopupElectrostaticsTexture = null!;
    private Texture2D _portalPopupOpticsTexture = null!;
    private Texture2D _portalPopupWavesTexture = null!;
    private Texture2D _portalPopupLaboratoryTexture = null!;
    private SpriteFont _font = null!;
    private SceneManager _sceneManager = null!;
    private InputState _input = null!;
    private GameServices _services = null!;
    private RenderTarget2D _sceneTarget = null!;
    private readonly bool _hostForWpf;
    private int _hostPresentationW;
    private int _hostPresentationH;
    private RenderTarget2D? _wpfPresentTarget;

    /// <param name="hostForWpf">Рендер в общую текстуру для WPF (D3DImage), без полноэкранного окна.</param>
    public Game1(bool hostForWpf = false)
    {
        _hostForWpf = hostForWpf;
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        Window.Title = "Physics Adventure Prototype";

        if (_hostForWpf)
        {
            HostedInputSource.IsActive = true;
            HostedInputSource.Reset();
            InactiveSleepTime = TimeSpan.Zero;
            IsFixedTimeStep = false;
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1;
            _graphics.PreferredBackBufferHeight = 1;
            _graphics.SynchronizeWithVerticalRetrace = false;
        }
        else
        {
            IsMouseVisible = false;
            _graphics.SynchronizeWithVerticalRetrace = true;
        }
    }

    /// <summary>Размер области презентации в пикселях (до инициализации — желаемый; после LoadContent пересоздаёт RT).</summary>
    public void ResizePresentation(int width, int height)
    {
        if (!_hostForWpf || width <= 0 || height <= 0)
        {
            return;
        }

        _hostPresentationW = width;
        _hostPresentationH = height;

        if (GraphicsDevice == null)
        {
            return;
        }

        if (_wpfPresentTarget != null &&
            _wpfPresentTarget.Width == width &&
            _wpfPresentTarget.Height == height)
        {
            return;
        }

        _wpfPresentTarget?.Dispose();
        _wpfPresentTarget = new RenderTarget2D(
            GraphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents,
            shared: true);
    }

    public IntPtr WpfSharedTextureHandle => _wpfPresentTarget?.GetSharedHandle() ?? IntPtr.Zero;

    public int PresentationPixelWidth => _wpfPresentTarget?.Width ?? 0;

    public int PresentationPixelHeight => _wpfPresentTarget?.Height ?? 0;

    protected override void Initialize()
    {
        _graphics.HardwareModeSwitch = false;

        if (_hostForWpf)
        {
            var pw = _hostPresentationW > 0 ? _hostPresentationW : VirtualResolution.Width;
            var ph = _hostPresentationH > 0 ? _hostPresentationH : VirtualResolution.Height;
            _graphics.PreferredBackBufferWidth = 1;
            _graphics.PreferredBackBufferHeight = 1;
            _graphics.ApplyChanges();
        }
        else
        {
            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = mode.Width;
            _graphics.PreferredBackBufferHeight = mode.Height;
            _graphics.ApplyChanges();

            Window.IsBorderless = true;
            Window.Position = new Point(0, 0);
        }

        base.Initialize();

        if (_hostForWpf)
        {
            var pw = _hostPresentationW > 0 ? _hostPresentationW : VirtualResolution.Width;
            var ph = _hostPresentationH > 0 ? _hostPresentationH : VirtualResolution.Height;
            VirtualResolution.SetPhysicalBackBufferSize(pw, ph);
        }
        else
        {
            var pp0 = GraphicsDevice.PresentationParameters;
            VirtualResolution.SetPhysicalBackBufferSize(pp0.BackBufferWidth, pp0.BackBufferHeight);
        }

        VirtualResolution.ApplyLetterboxViewport(GraphicsDevice);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _playerTexture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "player.png"));
        if (_playerTexture.Width > 1)
        {
            TextureHelper.PremultiplyAlpha(_playerTexture);
        }

        _menuCharacterTexture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "menu_character.png"));
        _menuBackgroundTexture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "menu_bg.png"));
        if (_menuBackgroundTexture.Width > 1)
        {
            TextureHelper.PremultiplyAlpha(_menuBackgroundTexture);
        }

        _portalInstallation01Texture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "portal_installation_01.png"));
        _portalInstallation02Texture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "portal_installation_02.png"));
        _portalInstallation03Texture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "portal_installation_03.png"));
        _portalInstallation04Texture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "portal_installation_04.png"));
        _portalInstallation05Texture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "portal_installation_05.png"));
        _portalInstallation06Texture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "portal_installation_06.png"));
        _taskIconTexture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "task_icon.png"));
        _interactionHintTexture = LoadTextureOrFallback(Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "interaction_hint.png"));
        if (_interactionHintTexture.Width > 1)
        {
            TextureHelper.PremultiplyAlpha(_interactionHintTexture);
        }

        _portalPopupMechanicsTexture = LoadPopupTexture("portal_popup_mechanics.png");
        _portalPopupElectrostaticsTexture = LoadPopupTexture("portal_popup_electrostatics.png");
        _portalPopupOpticsTexture = LoadPopupTexture("portal_popup_optics.png");
        _portalPopupWavesTexture = LoadPopupTexture("portal_popup_waves.png");
        _portalPopupLaboratoryTexture = LoadPopupTexture("portal_popup_laboratory.png");
        var worldLoadingPortalTexture = LoadTextureOrFallback(
            Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "world_loading_portal.png"));
        if (worldLoadingPortalTexture.Width > 1)
        {
            TextureHelper.PremultiplyAlpha(worldLoadingPortalTexture);
        }

        _font = Content.Load<SpriteFont>("Fonts/DefaultFont");

        _input = new InputState();
        var localization = new LocalizationService();
        var saveService = new SaveService();
        var saveData = saveService.Load();
        var dialog = new DialogSystem();
        var taskRepository = new TaskRepository(Path.Combine(AppContext.BaseDirectory, "Data", "tasks.json"));

        _services = new GameServices(
            _input,
            localization,
            saveService,
            saveData,
            _pixel,
            _playerTexture,
            _menuCharacterTexture,
            _menuBackgroundTexture,
            _portalInstallation01Texture,
            _portalInstallation02Texture,
            _portalInstallation03Texture,
            _portalInstallation04Texture,
            _portalInstallation05Texture,
            _portalInstallation06Texture,
            _taskIconTexture,
            _interactionHintTexture,
            _portalPopupMechanicsTexture,
            _portalPopupElectrostaticsTexture,
            _portalPopupOpticsTexture,
            _portalPopupWavesTexture,
            _portalPopupLaboratoryTexture,
            worldLoadingPortalTexture,
            _font,
            title => Window.Title = title,
            dialog,
            taskRepository,
            GraphicsDevice,
            _hostForWpf);
        _sceneManager = new SceneManager(_services);
        _sceneManager.ChangeScene(new MainMenuScene(_sceneManager, _services));

        _sceneTarget = new RenderTarget2D(
            GraphicsDevice,
            VirtualResolution.Width,
            VirtualResolution.Height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);

        if (_hostForWpf)
        {
            var pw = _hostPresentationW > 0 ? _hostPresentationW : VirtualResolution.Width;
            var ph = _hostPresentationH > 0 ? _hostPresentationH : VirtualResolution.Height;
            _wpfPresentTarget = new RenderTarget2D(
                GraphicsDevice,
                pw,
                ph,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.DiscardContents,
                shared: true);
        }
    }

    protected override void UnloadContent()
    {
        _wpfPresentTarget?.Dispose();
        _wpfPresentTarget = null;
        _sceneTarget?.Dispose();
        _sceneTarget = null!;
        base.UnloadContent();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _hostForWpf)
        {
            HostedInputSource.IsActive = false;
            HostedInputSource.Reset();
        }

        base.Dispose(disposing);
    }

    private Texture2D LoadTextureOrFallback(string path)
    {
        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            return Texture2D.FromStream(GraphicsDevice, stream);
        }

        return _pixel;
    }

    private Texture2D LoadPopupTexture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", fileName);
        return PortalPopupTextureLoader.Load(GraphicsDevice, path, _pixel);
    }

    protected override void Update(GameTime gameTime)
    {
        // Иначе PP/мышь могут остаться в координатах RT 1280×720 после Draw прошлого кадра
        GraphicsDevice.SetRenderTarget(null);
        if (_hostForWpf && _wpfPresentTarget != null)
        {
            VirtualResolution.SetPhysicalBackBufferSize(_wpfPresentTarget.Width, _wpfPresentTarget.Height);
        }
        else
        {
            var pp = GraphicsDevice.PresentationParameters;
            VirtualResolution.SetPhysicalBackBufferSize(pp.BackBufferWidth, pp.BackBufferHeight);
        }

        VirtualResolution.ApplyLetterboxViewport(GraphicsDevice);
        _input.Update(IsActive);
        var runSceneUpdate = true;
        // Внутри WPF лаборатория уже здесь; кнопка сверху справа дублировала запуск и мешала крестику закрытия.
        if (!_hostForWpf && IsActive && _input.IsLeftMouseJustPressed())
        {
            var m = _input.GetMouseVirtual(GraphicsDevice);
            if (GetLabButtonVirtualRect().Contains(m))
            {
                WpfBridgeLauncher.TryLaunch(out _);
                runSceneUpdate = false;
            }
        }

        // Пока фокус у другого приложения (терминал задач, лаборатория WPF, ForTeacher), не обновляем сцену — меньше нагрузка на GPU после переключения окон.
        if (runSceneUpdate && (_hostForWpf || IsActive))
        {
            _sceneManager.Update(gameTime);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_sceneTarget);
        GraphicsDevice.Viewport = new Viewport(0, 0, VirtualResolution.Width, VirtualResolution.Height);
        GraphicsDevice.Clear(Color.Black);
        _sceneManager.Draw(gameTime, _spriteBatch);

        int fullW;
        int fullH;

        if (_hostForWpf && _wpfPresentTarget != null)
        {
            GraphicsDevice.SetRenderTarget(_wpfPresentTarget);
            fullW = _wpfPresentTarget.Width;
            fullH = _wpfPresentTarget.Height;
        }
        else
        {
            GraphicsDevice.SetRenderTarget(null);
            fullW = GraphicsDevice.PresentationParameters.BackBufferWidth;
            fullH = GraphicsDevice.PresentationParameters.BackBufferHeight;
        }

        GraphicsDevice.Viewport = new Viewport(0, 0, fullW, fullH);
        GraphicsDevice.Clear(Color.Black);

        var dest = VirtualResolution.GetLetterboxDestinationRect(GraphicsDevice);
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        _spriteBatch.Draw(_sceneTarget, dest, Color.White);
        if (!_hostForWpf)
        {
            DrawLabButtonOverlay();
        }

        _spriteBatch.End();

        if (!_hostForWpf)
        {
            DrawSoftwareMousePointer(fullW, fullH);
        }

        if (_hostForWpf)
        {
            GraphicsDevice.SetRenderTarget(null);
        }

        base.Draw(gameTime);
    }

    private Rectangle GetLabButtonVirtualRect()
    {
        const int padX = 14;
        const int padY = 9;
        var label = _services.Localization.T("ui.labButton");
        var size = _font.MeasureString(label);
        var w = (int)Math.Ceiling(size.X) + padX * 2;
        var h = (int)Math.Ceiling(size.Y) + padY * 2;
        const int margin = 16;
        var x = VirtualResolution.Width - w - margin;
        var y = VirtualResolution.Height - h - margin;
        return new Rectangle(x, y, w, h);
    }

    private void DrawLabButtonOverlay()
    {
        var vRect = GetLabButtonVirtualRect();
        var pRect = VirtualResolution.VirtualRectToPhysical(vRect.X, vRect.Y, vRect.Width, vRect.Height);
        UIPrimitives.DrawRoundedPanel(_spriteBatch, _pixel, pRect, 8, new Color(12, 16, 28, 150));
        var label = _services.Localization.T("ui.labButton");
        var textSize = _font.MeasureString(label);
        var vTextX = vRect.X + (vRect.Width - textSize.X) / 2f;
        var vTextY = vRect.Y + (vRect.Height - textSize.Y) / 2f;
        var pos = VirtualResolution.VirtualPointToPhysical(vTextX, vTextY);
        var hover = _input.GetMouseVirtual(GraphicsDevice);
        var textColor = vRect.Contains(hover) ? UITheme.Accent : UITheme.TextPrimary;
        _spriteBatch.DrawString(_font, label, pos, textColor);
    }

    /// <summary>Указатель мыши в координатах физического backbuffer (SDL в borderless часто скрывает системный).</summary>
    private void DrawSoftwareMousePointer(int fullW, int fullH)
    {
        var m = _input.MousePosition;
        var sceneDest = VirtualResolution.GetLetterboxDestinationRect(GraphicsDevice);
        if (!sceneDest.Contains(m))
        {
            return;
        }

        var mx = Math.Clamp(m.X, sceneDest.Left, sceneDest.Right - 1);
        var my = Math.Clamp(m.Y, sceneDest.Top, sceneDest.Bottom - 1);

        GraphicsDevice.Viewport = new Viewport(0, 0, fullW, fullH);
        const int arm = 9;
        const int bar = 2;
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        var outline = new Color(8, 12, 20, 220);
        var fill = new Color(230, 242, 255);
        // Обводка (смещение на 1 px)
        for (var ox = -1; ox <= 1; ox++)
        {
            for (var oy = -1; oy <= 1; oy++)
            {
                if (ox == 0 && oy == 0)
                {
                    continue;
                }

                _spriteBatch.Draw(_pixel, new Rectangle(mx - arm + ox, my - bar / 2 + oy, arm * 2 + 1, bar), outline);
                _spriteBatch.Draw(_pixel, new Rectangle(mx - bar / 2 + ox, my - arm + oy, bar, arm * 2 + 1), outline);
            }
        }

        _spriteBatch.Draw(_pixel, new Rectangle(mx - arm, my - bar / 2, arm * 2 + 1, bar), fill);
        _spriteBatch.Draw(_pixel, new Rectangle(mx - bar / 2, my - arm, bar, arm * 2 + 1), fill);
        _spriteBatch.End();
    }
}
