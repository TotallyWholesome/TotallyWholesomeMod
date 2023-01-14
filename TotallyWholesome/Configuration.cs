using System;
using System.IO;
using Newtonsoft.Json;
using TotallyWholesome.Objects;
using TotallyWholesome.Objects.ConfigObjects;
using WholesomeLoader;

namespace TotallyWholesome
{
    public class Configuration
    {
        public static Config JSONConfig;
        public static string RootConfigPath = "UserData";
        public static string SettingsPath = Path.Combine(RootConfigPath, "TotallyWholesome");
        public static string AvatarConfigPath = Path.Combine(SettingsPath, "TWAvatarConfig");
        public static string ConfigFile = Path.Combine(SettingsPath, "TotallyWholesomeConfig.json");
        
        public static void Initialize()
        {
            if (!Directory.Exists(RootConfigPath))
                Directory.CreateDirectory(RootConfigPath);
            if (!Directory.Exists(AvatarConfigPath))
                Directory.CreateDirectory(AvatarConfigPath);
            if (File.Exists(ConfigFile))
            {
                try
                {
                    JSONConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFile));

                    if (JSONConfig == null)
                    {
                        JSONConfig = new Config();
                        SaveConfig();
                    }
                }
                catch (Exception e)
                {
                    Con.Error("Configuration file was not valid, resetting.");
                    Con.Error(e);
                    JSONConfig = new Config();
                    SaveConfig();
                }
            }
            else
            {
                JSONConfig = new Config();
                SaveConfig();
            }
        }
        public static void SaveConfig()
        {
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(JSONConfig, Formatting.Indented));
        }

        public static AvatarConfig LoadAvatarConfig(string avatarID)
        {
            if (!File.Exists(Path.Combine(AvatarConfigPath, avatarID + ".json")))
                return null;

            try
            {
                var avatarConf = JsonConvert.DeserializeObject<AvatarConfig>(File.ReadAllText(Path.Combine(AvatarConfigPath, avatarID + ".json")));
                return avatarConf;
            }
            catch
            {
                Con.Error($"Saved Avatar config for {avatarID} was not valid, config deleted!");
                File.Delete(Path.Combine(AvatarConfigPath, avatarID + ".json"));
            }

            return null;
        }

        public static void SaveAvatarConfig(string avatarID, AvatarConfig config)
        {
            File.WriteAllText(Path.Combine(AvatarConfigPath, avatarID + ".json"), JsonConvert.SerializeObject(config));
        }
    }
}
