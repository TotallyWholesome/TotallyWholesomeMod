#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using MelonLoader.Logging;
using Newtonsoft.Json;
using TotallyWholesome.Utils;

namespace TotallyWholesome.Managers.Shockers.OpenShock.Config;

public static class OpenShockConfig
{
    private const string ConfigPath = "UserData/TotallyWholesome/OpenShock.json";
    
    private static readonly MelonLogger.Instance Logger = new("TotallyWholesome.OpenShock.Config", ColorARGB.Green);
    private static OpenShockConf? _internalConfig;

    static OpenShockConfig()
    {
        TryLoad();
    }
    
    public static OpenShockConf Config => _internalConfig!;

    private static void TryLoad()
    {
        if (_internalConfig != null) return;
        if (File.Exists(ConfigPath))
        {
            var json = File.ReadAllText(ConfigPath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    _internalConfig = JsonConvert.DeserializeObject<OpenShockConf>(json);
                    Logger.Msg("Successfully loaded config");
                }
                catch (JsonException e)
                {
                    Logger.Error("Error during deserialization/loading of config", e);
                    File.Move(ConfigPath, ConfigPath + ".bak");
                    SaveNewConfig();
                    return;
                }
            }
        }

        if (_internalConfig != null) return;
        // No config found
        Logger.Msg("No config found, generating new one");
        SaveNewConfig();
    }

    private static void SaveNewConfig()
    {
        try
        {
            Logger.Msg("Generating new config file");
            _internalConfig = GetDefault();
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_internalConfig, Formatting.Indented));
        }
        catch (Exception e)
        {
            Logger.Error("Error occurred while generating new config file", e);
        }
    } 
    
    private static OpenShockConf GetDefault() => new()
    {
        ApiBaseUrl = new Uri("https://api.shocklink.net"),
        ApiToken = null
    };

    public static void SaveFnF()
    {
        TwTask.Run(SaveAsync);
    }
    
    private static readonly SemaphoreSlim SaveSemaphore = new(1, 1);

    public static async Task SaveAsync()
    {
        await SaveSemaphore.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(ConfigPath,
                JsonConvert.SerializeObject(_internalConfig, Formatting.Indented));
            Logger.Msg("Saved config file");
        }
        catch (Exception e)
        {
            Logger.Error("Error occurred while saving new config file", e);
        }
        finally
        {
            SaveSemaphore.Release();
        }
    }
    
    public sealed class OpenShockConf
    {
        public Uri ApiBaseUrl { get; set; } = null!;
        public string? ApiToken { get; set; }

        public Dictionary<Guid, ShockerConfig> Shockers { get; set; } = new();
    
        public sealed class ShockerConfig
        {
            public bool Enabled { get; set; } = false;

            public ushort LimitDuration { get; set; } = 15000;
            public byte LimitIntensity { get; set; } = 100;

            public bool AllowShock { get; set; } = true;
            public bool AllowVibrate { get; set; } = true;
            public bool AllowSound { get; set; } = true;
        }
    }
}