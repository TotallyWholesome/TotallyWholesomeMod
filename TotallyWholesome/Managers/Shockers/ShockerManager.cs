#nullable enable
using System;
using System.Threading.Tasks;
using ABI_RC.Systems.UI.UILib;
using MelonLoader;
using MelonLoader.Logging;
using OneOf;
using OneOf.Types;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers.OpenShock;
using TotallyWholesome.Managers.Shockers.PiShock;
using TotallyWholesome.Network;
using TotallyWholesome.Objects;
using TotallyWholesome.Utils;
using TWNetCommon.Data.ControlPackets.Shockers;
using TWNetCommon.Data.ControlPackets.Shockers.Models;


namespace TotallyWholesome.Managers.Shockers;

public sealed class ShockerManager : ITWManager
{
    private static readonly MelonLogger.Instance Logger =
        new("TotallyWholesome ShockerManager", ColorARGB.Green);

    public static ShockerManager Instance { get; private set; } = null!;
    public IShockerProvider? ShockerProvider { get; private set; }
    public HeightControl? HeightControl { get; private set; }

    public int Priority => 5;

    #region Achievment data

    // Sending side (master)
    public DateTime LastActionTimeMaster { get; private set; }
    public ControlType LastControlTypeMaster { get; private set; }
    public byte LastIntensityMaster { get; private set; }
    public uint LastDurationMaster { get; private set; }


    // Receiving side (pet)
    public ControlType LastControlTypePet { get; private set; }
    public byte LastIntensityPet { get; private set; }
    public uint LastDurationPet { get; private set; }
    public DateTime LastActionTimePet { get; private set; }

    #endregion

    /// <summary>
    /// Called by TW setup
    /// </summary>
    public void Setup()
    {
        Instance = this;

        LeadManager.OnLeadPairDestroyed += OnLeadRemoveEvent;

        SetupPlatform();
    }

    /// <summary>
    /// Event method for when leash is removed
    /// </summary>
    /// <param name="obj"></param>
    private void OnLeadRemoveEvent(LeadPair obj)
    {
        Reset();
    }

    /// <summary>
    /// Reset all states related to the masters control
    /// </summary>
    private void Reset()
    {
        HeightControl = null;
    }

    /// <summary>
    /// Reload for when the platform changes
    /// </summary>
    public void Reload()
    {
        SetupPlatform();
    }

    /// <summary>
    /// Select a shocker platform, can also be used to reset to none
    /// </summary>
    /// <param name="platform"></param>
    public void SelectPlatform(Config.ShockerPlatform platform)
    {
        Configuration.JSONConfig.SelectedShockerPlatform = platform;
        Configuration.SaveConfig();

        Reload();
    }

    private void SetupPlatform()
    {
        var prevProvider = ShockerProvider;

        TwTask.Run(async () =>
        {
            if (prevProvider is not IAsyncDisposable disposable) return;
            await disposable.DisposeAsync();
        });

        ShockerProvider = null;

        Logger.Msg("Selected Shocker Platform: " + Configuration.JSONConfig.SelectedShockerPlatform);

        switch (Configuration.JSONConfig.SelectedShockerPlatform)
        {
            case Config.ShockerPlatform.OpenShock:
                ShockerProvider = new OpenShockManager();
                break;
            case Config.ShockerPlatform.PiShock:
                ShockerProvider = new PiShockManager();
                break;
            case Config.ShockerPlatform.None:
            default:
                return;
        }
    }

    public void LateSetup()
    {
    }

    /// <summary>
    /// Send a shock to your pets
    /// </summary>
    /// <param name="type"></param>
    /// <param name="intensity"></param>
    /// <param name="duration"></param>
    /// <param name="petPair">Optional pet lead for IPC</param>
    public Task SendControlNetworked(ControlType type, byte intensity, ushort duration, LeadPair? petPair = null)
    {
        LastActionTimeMaster = DateTime.UtcNow;
        LastIntensityMaster = intensity;
        LastDurationMaster = duration;
        LastControlTypeMaster = type;
        return TWNetSendHelpers.SendShockControl(type, intensity, duration, petPair);
    }


    // TODO: Rate limit to 10 times a second
    /// <summary>
    /// Send a height control to your pets
    /// </summary>
    /// <param name="enabled"></param>
    /// <param name="height"></param>
    /// <param name="strengthMin"></param>
    /// <param name="strengthMax"></param>
    /// <param name="strengthStep"></param>
    /// <param name="petPair"></param>
    /// <returns></returns>
    public Task SendHeightControlNetworked(bool enabled, float height, float strengthMin, float strengthMax,
        float strengthStep, LeadPair? petPair = null) =>
        TWNetSendHelpers.SendHeightControl(enabled, height, strengthMin, strengthMax, strengthStep, petPair);

    /// <summary>
    /// Received a shock from your master or local trigger, logging and ui feedback on response
    /// </summary>
    /// <param name="type"></param>
    /// <param name="intensity"></param>
    /// <param name="duration"></param>
    public async Task UiControl(ControlType type, byte intensity, ushort duration)
    {
        var control = await Control(type, intensity, duration);
        control.Switch(success => { },
            notAllowed =>
            {
                Logger.Msg($"Not allowed to control shockers with type: {type}");
                QuickMenuAPI.ShowAlertToast("Not allowed to control shockers with type: " + type);
            },
            noPlatformEnabled =>
            {
                Logger.Msg("No platform enabled to control shocker"); 
                QuickMenuAPI.ShowAlertToast("No platform enabled to control shocker");
            });
    }

    /// <summary>
    /// Received a shock from your master or local trigger
    /// </summary>
    /// <param name="type"></param>
    /// <param name="intensity"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public async Task<OneOf<Success, NotAllowed, NoPlatformEnabled>> Control(ControlType type, byte intensity,
        ushort duration)
    {
        if (ShockerProvider == null) return new NoPlatformEnabled();
        if (!ConfigManager.Instance.IsActiveCurrent(AccessType.AllowShockControl)) return new NotAllowed();
        if (!CheckPermission(type)) return new NotAllowed();

        await ShockerProvider.Control(type, intensity, duration);
        AvatarParameterManager.Instance.TrySetTemporaryParameter("TWShockerShock", 1f, 0f, duration / 1000f);
        return new Success();
    }

    private static bool CheckPermission(ControlType type) => type switch
    {
        ControlType.Stop => ConfigManager.Instance.IsActiveCurrent(AccessType.AllowShock) ||
                            ConfigManager.Instance.IsActiveCurrent(AccessType.AllowVibrate) ||
                            ConfigManager.Instance.IsActiveCurrent(AccessType.AllowBeep),

        ControlType.Shock => ConfigManager.Instance.IsActiveCurrent(AccessType.AllowShock),
        ControlType.Vibrate => ConfigManager.Instance.IsActiveCurrent(AccessType.AllowVibrate),
        ControlType.Sound => ConfigManager.Instance.IsActiveCurrent(AccessType.AllowBeep),
        _ => false
    };

    /// <summary>
    /// Received a height control from your master
    /// </summary>
    /// <param name="heightControl"></param>
    public void Height(HeightControl heightControl)
    {
        HeightControl = heightControl;
    }
}

public struct NotAllowed;

public struct NoPlatformEnabled;