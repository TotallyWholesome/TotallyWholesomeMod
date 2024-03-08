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
using Semver;
using UnityEngine;

namespace WholesomeLoader
{
    public static class LoaderBuildInfo
    {
        public const string Name = "WholesomeLoader";
        public const string Author = "Totally Wholesome Team";
        public const string Company = "TotallyWholesome";
        public const string Version = "3.3.5";
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
        private const string URL = "https://api.potato.moe/api-tw/v3/getAssembly"; // Mod DLL
        private const string VersionsURL = "https://api.potato.moe/api-tw/v3/getAssyVersions"; //Versions endpoint
        
        private string _currentVersion;
        private bool _paranoidMode;
        
        private Config _configJson;
        private TWAssemblyVersion _selectedVersion = new();

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
                    Con.Msg(System.ConsoleColor.DarkYellow, "Loading Local Mod");
                    var assyFile = new FileInfo("TotallyWholesome.dll");
                    if (assyFile.Exists)
                    {
                        Con.Msg("Found local TotallyWholesome.dll!");
                        twAssembly = Assembly.LoadFile(assyFile.FullName);
                    }
                    else
                    {
                        Con.Error("No local TotallyWholesome.dll found! Skipping local load check.");
                    }
                        
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
                    //TODO: fix the lack of null check after deserialize, oops.
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

            if(twAssembly == null)
                if (!CheckCompatibility())
                    return;

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

        private bool CheckCompatibility()
        {
            var mlVersion = SemVersion.Parse(BuildInfo.Version);
            var cvrVersion = new CVRVersion(Application.version);

            //Check all available branches for compatibility
            foreach (var version in AvailableVersions)
            {
                if(!SemVersion.TryParse(version.MinMLVersion, out var minMLVersion))
                    continue;
                if(mlVersion.CompareByPrecedence(minMLVersion) < 0)
                    continue;
                version.IsMLCompatible = true;
                if(cvrVersion.IsVersionNewer(version.MinVer) < 0 || cvrVersion.IsVersionNewer(version.MaxVer) > 0)
                    continue;
                version.IsCVRCompatible = true;
                Con.Debug($"Version: {version.TWVersionInfo} | Branch: {version.BranchPrettyName} is compatible with this game/ml configuration");
            }

            if (!_selectedVersion.IsCVRCompatible || !_selectedVersion.IsMLCompatible)
            {
                //Selected version is not compatible, let's warn the user!
                if (!_selectedVersion.IsMLCompatible)
                    Con.Warn($"Selected branch {_selectedVersion.BranchPrettyName} with version {_selectedVersion.TWVersionInfo} is not listed as compatible with this version of MelonLoader! Minimum required version: {_selectedVersion.MinMLVersion}");
                if (!_selectedVersion.IsCVRCompatible)
                    Con.Warn($"Selected branch {_selectedVersion.BranchPrettyName} with version {_selectedVersion.TWVersionInfo} is not listed as compatible with this version of ChilloutVR! Expected minimum version: {_selectedVersion.ExpectedCVRVersion}");
                
                Con.Msg("Checking for available compatible versions...");
                var compatibleVersions = AvailableVersions.Where(x => x.IsMLCompatible && x.IsCVRCompatible).ToArray();
                if (compatibleVersions.Length > 1)
                {
                    Con.Msg("You have multiple branches that are compatible with this version, the first one will be selected.");
                    foreach (var version in compatibleVersions) Con.Msg($"Compatible Branch: {version.BranchPrettyName} | TW Version: {version.TWVersionInfo}");
                }

                if (compatibleVersions.Length > 0)
                {
                    _selectedVersion = compatibleVersions[0];
                    Con.Msg($"Selected branch {_selectedVersion.BranchPrettyName} with version {_selectedVersion.TWVersionInfo}!");
                }
                else
                {
                    //No compatible versions found, alert user and allow bypass to load incompatible version
                    Con.Error("No versions of TotallyWholesome that are compatible with these MelonLoader/ChilloutVR versions could be found!");
                    if (!ConsoleConfirmation("Would you like to launch anyways? This version may not work as intended!"))
                        return false;
                }
            }

            return true;
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
                            _selectedVersion = selectedBranch;
                            Con.Msg($"Retrieved assembly information for {selectedBranch.BranchPrettyName}!");
                        }

                        if (_selectedVersion.TWVersionInfo != null)
                        {
                            if (_currentVersion.Equals(_selectedVersion.TWVersionInfo))
                                return true;

                            _currentVersion = _selectedVersion.TWVersionInfo;
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
                if (_selectedVersion.AssemblyHash != null && (md5Sum.Equals(_selectedVersion.AssemblyHash) || string.IsNullOrWhiteSpace(_selectedVersion.AssemblyHash)))
                {
                    Con.Msg("\u001b[0m[\u001b[34mSUCCESS\u001b[0m] \u001b[36mLoaded cached TotallyWholesome assembly!\u001b[0m");
                    twAssy = Assembly.Load(File.ReadAllBytes(NewConfigRoot + "TotallyWholesome.dll"));
                    return twAssy;
                }
            }
            
            if (_paranoidMode)
            {
                Con.Warn($"[TWParanoidMode] Your Totally Wholesome is out of date or your local copy is missing/damaged! Local Hash: {md5Sum} | Expected Hash: {_selectedVersion.AssemblyHash}");
                Con.Warn($"[TWParanoidMode] Would you like to update to the new version? Version Info: {_selectedVersion.TWVersionInfo} | Branch: {_selectedVersion.BranchPrettyName}");
                if (!ConsoleConfirmation("Would you like to update? (Enter yes or no): "))
                {
                    Con.Warn("[TWParanoidMode] You have chosen to not update, do note that you WILL NOT receive support while on an outdated version of Totally Wholesome, you may also encounter issues connecting to and using TWNet while on an outdated version!");

                    if (!File.Exists(NewConfigRoot + "TotallyWholesome.dll")) return null;
                    
                    _selectedVersion.AssemblyHash = md5Sum;
                    return getTW_Assy(true);
                }
            }

            using HttpClient client = new HttpClient();
            
            client.DefaultRequestHeaders.Add("User-Agent", "WholesomeLoader");

            try
            {
                string finalUrl = URL;

                if(!string.IsNullOrWhiteSpace(_configJson.LoginKey) && !string.IsNullOrWhiteSpace(_configJson.SelectedBranch))
                    finalUrl += $"/{_configJson.LoginKey}/{_selectedVersion.Branch}";

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
                Con.Msg(System.ConsoleColor.Red, "TotallyWholesome says bye bye!");
            }
        }
    }

    public class TWAssemblyVersion
    {
        public string Branch = "live";
        public string BranchPrettyName = "Live";
        public string TWVersionInfo = "No name";
        public string AssemblyName;
        public string AssemblyHash;
        public string MinMLVersion;
        public bool IsCVRCompatible = false;
        public bool IsMLCompatible = false;
        public CVRVersion MinVer;
        public CVRVersion MaxVer;

        public string ExpectedCVRVersion
        {
            set => MinVer = new CVRVersion(value);
            get => MinVer?.VersionString;
        }

        public string MaxCVRVersion
        {
            set => MaxVer = new CVRVersion(value);
            get => MaxVer?.VersionString;
        }
        
        

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(AssemblyHash) && !string.IsNullOrWhiteSpace(AssemblyName);
        }
    }
    
    class Config
    {
        public string SelectedBranch = "live";
        public string LoginKey = "";
    }
}
