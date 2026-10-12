using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Dialog;
using PhisicsGame.Core.Input;
using PhisicsGame.Core.Localization;
using PhisicsGame.Core.Save;
using PhisicsGame.Core.Tasks;

namespace PhisicsGame.Core;

public sealed class GameServices(
    InputState input,
    LocalizationService localization,
    SaveService saveService,
    SaveData saveData,
    Texture2D pixel,
    Texture2D playerTexture,
    Texture2D menuCharacterTexture,
    Texture2D menuBackgroundTexture,
    Texture2D portalInstallation01Texture,
    Texture2D portalInstallation02Texture,
    Texture2D portalInstallation03Texture,
    Texture2D portalInstallation04Texture,
    Texture2D portalInstallation05Texture,
    Texture2D portalInstallation06Texture,
    Texture2D taskIconTexture,
    Texture2D interactionHintTexture,
    Texture2D portalPopupMechanicsTexture,
    Texture2D portalPopupElectrostaticsTexture,
    Texture2D portalPopupOpticsTexture,
    Texture2D portalPopupWavesTexture,
    Texture2D portalPopupLaboratoryTexture,
    Texture2D worldLoadingPortalTexture,
    SpriteFont font,
    Action<string> setWindowTitle,
    DialogSystem dialog,
    TaskRepository taskRepository,
    GraphicsDevice graphicsDevice,
    bool hostedInWpfLab = false)
{
    public GraphicsDevice GraphicsDevice { get; } = graphicsDevice;
    public InputState Input { get; } = input;
    public LocalizationService Localization { get; } = localization;
    public SaveService SaveService { get; } = saveService;
    public SaveData SaveData { get; } = saveData;
    public Texture2D Pixel { get; } = pixel;
    public Texture2D PlayerTexture { get; } = playerTexture;
    public Texture2D MenuCharacterTexture { get; } = menuCharacterTexture;
    public Texture2D MenuBackgroundTexture { get; } = menuBackgroundTexture;
    public Texture2D PortalInstallation01Texture { get; } = portalInstallation01Texture;
    public Texture2D PortalInstallation02Texture { get; } = portalInstallation02Texture;
    public Texture2D PortalInstallation03Texture { get; } = portalInstallation03Texture;
    public Texture2D PortalInstallation04Texture { get; } = portalInstallation04Texture;
    public Texture2D PortalInstallation05Texture { get; } = portalInstallation05Texture;
    public Texture2D PortalInstallation06Texture { get; } = portalInstallation06Texture;
    public Texture2D TaskIconTexture { get; } = taskIconTexture;
    public Texture2D InteractionHintTexture { get; } = interactionHintTexture;
    public Texture2D PortalPopupMechanicsTexture { get; } = portalPopupMechanicsTexture;
    public Texture2D PortalPopupElectrostaticsTexture { get; } = portalPopupElectrostaticsTexture;
    public Texture2D PortalPopupOpticsTexture { get; } = portalPopupOpticsTexture;
    public Texture2D PortalPopupWavesTexture { get; } = portalPopupWavesTexture;
    public Texture2D PortalPopupLaboratoryTexture { get; } = portalPopupLaboratoryTexture;
    public Texture2D WorldLoadingPortalTexture { get; } = worldLoadingPortalTexture;
    public SpriteFont Font { get; } = font;
    public Action<string> SetWindowTitle { get; } = setWindowTitle;
    public DialogSystem Dialog { get; } = dialog;
    public TaskRepository TaskRepository { get; } = taskRepository;

    /// <summary>Игра встроена в окно WPF-лаборатории (отдельный exe лаборатории не запускаем).</summary>
    public bool HostedInWpfLab { get; } = hostedInWpfLab;

    public Texture2D GetPortalHubPopupTexture(string topicId) =>
        topicId switch
        {
            "mechanics" => PortalPopupMechanicsTexture,
            "electrostatics" => PortalPopupElectrostaticsTexture,
            "optics" => PortalPopupOpticsTexture,
            "waves" => PortalPopupWavesTexture,
            _ => Pixel
        };
}
