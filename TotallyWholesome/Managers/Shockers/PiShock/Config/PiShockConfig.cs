#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using MelonLoader.Logging;
using Newtonsoft.Json;
using TotallyWholesome.Utils;

namespace TotallyWholesome.Managers.Shockers.PiShock.Config;

public static class PiShockConfig
{
    private const string ConfigPath = "UserData/TotallyWholesome/PiShock.json";
    
    private static readonly MelonLogger.Instance Logger = new("TotallyWholesome.PiShock.Config", ColorARGB.Green);
    private static PiShockConf? _internalConfig;

    static PiShockConfig()
    {
        TryLoad();
    }
    
    public static PiShockConf Config => _internalConfig!;

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
                    _internalConfig = JsonConvert.DeserializeObject<PiShockConf>(json);
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
    
    private static PiShockConf GetDefault() => new();

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
    
    public sealed class PiShockConf
    {
        public Dictionary<string, ShockerConfig> Shockers { get; set; } = new();
    
        public sealed class ShockerConfig
        {
            public bool Enabled { get; set; } = false;
        }
    }
}