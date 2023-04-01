using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WholesomeLoader
{
    public static class LoaderBuildInfo
    {
        public const string Name = "WholesomeLoader";
        public const string Author = "Totally Wholesome Team";
        public const string Company = "TotallyWholesome";
        public const string Version = "3.2.0";
        public const string DownloadLink = "https://totallywholeso.me/downloads/WholesomeLoader.dll";
    }

    public class WholesomeLoader : MelonMod
    {
        public static TWAssemblyVersion[] AvailableVersions;
        
        private MelonMod _wholesomeLoaderV2;
        private static bool _hasQuit;
        private const string RootConfigPath = "UserData/";
        private const string NewConfigRoot = "UserData/TotallyWholesome/";
        private const string ConfigFile = "TotallyWholesomeConfig.json";
        private const string URL = "https://api.potato.moe/api-tw/v2/getAssembly"; // Mod DLL
        private const string VersionsURL = "https://api.potato.moe/api-tw/v2/getAssyVersions"; //Versions endpoint
        
        private string _currentVersion;
        private bool _paranoidMode;

        private string _targetBranch = "live";
        private string _targetBranchPrettyName = "Live";
        private string _twVersionInfo = "No Name";
        private string _newVersionName;
        private string _newVersionHash;
        private Config _configJson;

        public override void OnInitializeMelon()
        {
            Con.Msg("Welcome to WholesomeLoader!");

            if (!Directory.Exists(NewConfigRoot))
                Directory.CreateDirectory(NewConfigRoot);

            LoadVersionData();

            Assembly twAssembly = null;

            if (Environment.CommandLine.Contains("--WholesomeDev") || Environment.CommandLine.Contains("--tw.debug"))
                Con.DebugMode = true;

            if (Environment.CommandLine.Contains("--WholesomeDev"))
            {
                try {
                    Con.Msg(ConsoleColor.Yellow, "Loading Local Mod");
                    twAssembly = Assembly.LoadFile("TotallyWholesome.dll");
                } catch (Exception e) {
                    Con.Error(e);
                    Con.Warn("Could not load Local Mod, loading TotallyWholesome from the server.");
                }
            }

            _paranoidMode = Environment.CommandLine.Contains("--TWParanoidMode");

            if (File.Exists(NewConfigRoot + ConfigFile))
            {
                try
                {
                    _configJson = JsonConvert.DeserializeObject<Config>(File.ReadAllText(NewConfigRoot + ConfigFile));
                }
                catch (Exception e)
                {
                    Con.Error("Your TotallyWholesomeConfig.json is invalid or corrupted! It has been renamed to TotallyWholesomeConfig.json.old and a new config has been generated!");
                    Con.Error(e);
                    
                    File.Move(NewConfigRoot + ConfigFile, NewConfigRoot + ConfigFile + ".old");

                    _configJson = new Config();
                }
            }
            else
            {
                _configJson = new Config();
            }

            if (_configJson != null && !string.IsNullOrWhiteSpace(_configJson.SelectedBranch) && !_configJson.SelectedBranch.Equals("live", StringComparison.InvariantCultureIgnoreCase))
                Con.Msg($"WholesomeLoader will attempt to load branch {_configJson.SelectedBranch}! If you find issues please report them in the appropriate channels!");

            var loadCached = CheckNewAssemblyVersion();
            
            if (twAssembly == null)
            {
                //We want to check assembly versions now
                twAssembly = getTW_Assy(loadCached);
            }

            if (twAssembly != null)
            {
                LoadModuleCore(twAssembly);
                return;
            }
            
            Con.Error("Unable to load Totally Wholesome, no assembly was found.\nPlease check the discord for more information!");
        }

        private void LoadModuleCore(Assembly assy)
        {
            if (assy == null) return;

            var melonAssy = MelonAssembly.LoadMelonAssembly("", assy);
            RegisterSorted(melonAssy.LoadedMelons);

            var mod = RegisteredMelons.FirstOrDefault(x => x.Info.Name.Equals("TotallyWholesome"));

            if (mod == null)
            {
                Con.Error("Unable to start TotallyWholesome! MelonLoader did not load the assembly!");
            }
            else
            {
                Con.Msg("MelonLoader has successfully loaded the TotallyWholesome assembly! Starting up!");
                mod.OnInitializeMelon();
            }
        }
        
        private void WriteVersionData(string newVersion)
        { 
            File.WriteAllBytes(NewConfigRoot + "\\version.dat", Encoding.UTF8.GetBytes(newVersion));
        }

        private void LoadVersionData()
        {
            _currentVersion = File.Exists(NewConfigRoot + "\\version.dat") ? Encoding.UTF8.GetString(File.ReadAllBytes(NewConfigRoot + "\\version.dat")) : "none";
        }

        private bool CheckNewAssemblyVersion()
        {
            using HttpClient client = new HttpClient();
            
            client.DefaultRequestHeaders.Add("User-Agent", "WholesomeLoader");

            try
            {
                var targetUrl = VersionsURL;

                if (_configJson != null)
                    targetUrl += $"/{_configJson.LoginKey}";
                
                Task<HttpResponseMessage> versionRequest = client.GetAsync(targetUrl);
                versionRequest.Wait();

                HttpResponseMessage message = versionRequest.Result;

                switch (message.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var stringTask = message.Content.ReadAsStringAsync();
                        stringTask.Wait();
                        AvailableVersions = JsonConvert.DeserializeObject<TWAssemblyVersion[]>(stringTask.Result);
                        if (AvailableVersions == null)
                            return false;

                        var selectedBranch = AvailableVersions.FirstOrDefault(x => x.Branch.Equals(_configJson.SelectedBranch, StringComparison.InvariantCultureIgnoreCase)) ?? AvailableVersions.FirstOrDefault(x => x.Branch.Equals("live", StringComparison.InvariantCultureIgnoreCase));

                        if (selectedBranch != null && selectedBranch.AssemblyHash != null && selectedBranch.AssemblyName != null)
                        {
                            _targetBranch = selectedBranch.Branch;
                            _targetBranchPrettyName = selectedBranch.BranchPrettyName;
                            _newVersionName = selectedBranch.AssemblyName;
                            _newVersionHash = selectedBranch.AssemblyHash;

                            if (selectedBranch.TWVersionInfo != null)
                                _twVersionInfo = selectedBranch.TWVersionInfo;
                            
                            Con.Msg($"Retrieved assembly information for {selectedBranch.BranchPrettyName}!");
                        }

                        if (_newVersionName != null)
                        {
                            if (_currentVersion.Equals(_newVersionName))
                                return true;

                            _currentVersion = _newVersionName;
                            return false;
                        }

                        break;
                    default:
                        Con.Error($"An unknown error has occured! HTTP Status Code: {message.StatusCode}\nUnable to retrieve TW assembly versions!");
                        break;
                }
            }
            catch (Exception e)
            {
                Con.Error("Unable to retrieve TW assembly versions from api!");
                Con.Error(e);
            }

            return false;
        }

        private Assembly getTW_Assy(bool loadCached)
        {
            Assembly twAssy = null;

            string md5Sum = null;
            
            if(File.Exists(NewConfigRoot + "TotallyWholesome.dll"))
                md5Sum = GetMD5(NewConfigRoot + "TotallyWholesome.dll");

            if (loadCached && md5Sum != null)
            {
                if (_newVersionHash != null && (md5Sum.Equals(_newVersionHash) || string.IsNullOrWhiteSpace(_newVersionHash)))
                {
                    Con.Msg("\u001b[0m[\u001b[34mSUCCESS\u001b[0m] \u001b[36mLoaded cached TotallyWholesome assembly!\u001b[0m");
                    twAssy = Assembly.Load(File.ReadAllBytes(NewConfigRoot + "TotallyWholesome.dll"));
                    return twAssy;
                }
            }
            
            if (_paranoidMode)
            {
                Con.Warn($"[TWParanoidMode] Your Totally Wholesome is out of date or your local copy is missing/damaged! Local Hash: {md5Sum} | Expected Hash: {_newVersionHash}");
                Con.Warn($"[TWParanoidMode] Would you like to update to the new version? Version Info: {_twVersionInfo} | Branch: {_targetBranchPrettyName}");
                if (!ConsoleConfirmation("Would you like to update? (Enter yes or no): "))
                {
                    Con.Warn("[TWParanoidMode] You have chosen to not update, do note that you WILL NOT receive support while on an outdated version of Totally Wholesome, you may also encounter issues connecting to and using TWNet while on an outdated version!");

                    if (!File.Exists(NewConfigRoot + "TotallyWholesome.dll")) return null;
                    
                    _newVersionHash = md5Sum;
                    return getTW_Assy(true);
                }
            }

            using HttpClient client = new HttpClient();
            
            client.DefaultRequestHeaders.Add("User-Agent", "WholesomeLoader");

            try
            {
                string finalUrl = URL;

                if(!string.IsNullOrWhiteSpace(_configJson.LoginKey) && !string.IsNullOrWhiteSpace(_configJson.SelectedBranch))
                    finalUrl += $"/{_configJson.LoginKey}/{_targetBranch}";

                Task<HttpResponseMessage> assyRequest = client.GetAsync(finalUrl);
                assyRequest.Wait();
                HttpResponseMessage message = assyRequest.Result;
                switch (message.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Con.Msg("\u001b[0m[\u001b[34mSUCCESS\u001b[0m] \u001b[36mSuccessfully retrieved TotallyWholesome assembly!\u001b[0m");
                        Task<byte[]> twBytes = message.Content.ReadAsByteArrayAsync();
                        twBytes.Wait();
                        File.WriteAllBytes(NewConfigRoot + "TotallyWholesome.dll", twBytes.Result);
                        WriteVersionData(_currentVersion);
                        twAssy = Assembly.Load(twBytes.Result);
                        break;
                    case HttpStatusCode.InternalServerError:
                        Con.Error($"Something went wrong with the Totally Wholesome assembly fetch! Please check the Discord and report this error! ({message.StatusCode})");
                        break;
                    case HttpStatusCode.NotFound:
                        Con.Error("Unable to retrieve Totally Wholesome assembly! Please check the discord for details or check if your beta key is invalid!");
                        break;
                    default:
                        Con.Error($"An unknown error has occured! HTTP Status Code: {message.StatusCode} | Unable to load TotallyWholesome!");
                        break;
                }
            }
            catch(Exception e)
            {
                Con.Error("Unable to retrieve Totally Wholesome! Unable to start!");
                Con.Error(e);
            }

            return twAssy;
        }

        private static string GetMD5(string file)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(file);
            var hash2 = md5.ComputeHash(stream);
            return BitConverter.ToString(hash2).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static bool ConsoleConfirmation(string prompt)
        {
            Con.Msg(prompt);
            var entry = Console.ReadLine();

            if (entry != null && (entry.Trim().Equals("yes", StringComparison.InvariantCultureIgnoreCase) || entry.Trim().Equals("no", StringComparison.InvariantCultureIgnoreCase)))
            {
                return entry.Trim().Equals("yes", StringComparison.InvariantCultureIgnoreCase);
            }

            return ConsoleConfirmation(prompt);
        }

        public override void OnApplicationQuit()
        {
            if (!_hasQuit) {
                _hasQuit = true;
                Con.Msg(ConsoleColor.Red, "TotallyWholesome says bye bye!");
            }
        }
    }

    public class TWAssemblyVersion
    {
        public string Branch;
        public string BranchPrettyName;
        public string TWVersionInfo;
        public string AssemblyName;
        public string AssemblyHash;
    }
    
    class Config
    {
        public string SelectedBranch = "live";
        public string LoginKey = "";
    }
}
