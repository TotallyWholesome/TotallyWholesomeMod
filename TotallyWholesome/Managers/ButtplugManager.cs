using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ABI_RC.Core.Savior;
using ButtplugManaged;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TotallyWholesome.TWUI;
using TWNetCommon.Data;
using WholesomeLoader;

namespace TotallyWholesome.Managers
{
    public class ButtplugManager : ITWManager
    {

        public static ButtplugManager Instance;
        public ButtplugClient buttplugClient;
        private Process intifaceProcess;
        private int port = 12344;
        private string buttplugCLIPath;

        private HashSet<LeadPair> queue = new HashSet<LeadPair>();
        
        private bool VibeGoBrrCompatMode = false;

        //forallpets
        public SliderFloat ToyStrength;
        public SliderFloat ToyStrengthIPC;


        //VGB Integration
        private MelonMod vgb;
        private FieldInfo mIdleIntensity;
        private FieldInfo mIdleEnabled;

        public string ManagerName() => nameof(ButtplugManager);
        public int Priority() => 1;
        public void Setup()
        {
            Instance = this;

            ToyStrength = new SliderFloat("lovenseStrengthSlider", 0f);
            ToyStrength.OnValueUpdated += f => { LeadSenders.SendMasterRemoteSettingsAsync(); };

            ToyStrengthIPC = new SliderFloat("lovenseStrengthSliderIPC", 0f);
            ToyStrengthIPC.OnValueUpdated += f =>
            {
                var leadPair = UserInterface.Instance.SelectedLeadPair;
                
                if (leadPair == null)
                    return;

                leadPair.ToyStrength = f;
                LeadSenders.SendMasterRemoteSettingsAsync(leadPair);
            };
            

            //ToChange
            if (!ConfigManager.Instance.IsActive(AccessType.EnableToyControl))
                return;

            TWNetListener.MasterRemoteControlEvent += OnMasterRemoteControlEvent;
            TWNetListener.LeadRemoveEvent += OnLeadRemoveEvent;

            VibeGoBrrCompatMode = MelonMod.RegisteredMelons.Any(x => x.Info.Name == "VibeGoesBrrr");
            if (VibeGoBrrCompatMode)
            {
                LoadVibeGoBrrrIntegration();
                return;
            }

            buttplugCLIPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IntifaceDesktop", "engine", "IntifaceCLI.exe");
            var buttplugConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IntifaceDesktop", "intiface.config.json");
            if (!File.Exists(buttplugCLIPath))
            {
                Con.Msg("Intiface Desktop is not installed");
                // download it from https://github.com/intiface/intiface-cli-rs/releases/latest/download/intiface-cli-rs-win-x64-Release.zip
                DownloadButtplugCLI();
            }
            else
            {
                Con.Msg("Intiface Desktop located using installed version");
                port = (int)JObject.Parse(File.ReadAllText(buttplugConfigPath)).GetValue("websocketServerInsecurePort");
                Con.Msg($"Using port {port} of Intiface installation");
            }

            try
            {
                StartButtplugInstance();
            }
            catch (Exception e)
            {
                Con.Error("Failed to start Buttplug CLI");
                Con.Error(e);
            }
        }
        
        public void LateSetup(){}

        private void OnLeadRemoveEvent(LeadAccept packet)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (packet.FollowerID != MetaPort.Instance.ownerId)
                    return;

                ResetToys();
            });
        }

        private void DownloadButtplugCLI()
        {
            Con.Msg("Checking if Intiface needs to be updated...");

            using var wc = new WebClient
            {
                Headers = {
                    ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0"
                }
            };

            if (CheckForIntifaceUpdate(wc) || !File.Exists("Executables/IntifaceCLI.exe"))
            {
                Con.Msg("New Intiface version detected! Downloading to Executables/IntifaceCLI.exe!");

                try
                {
                    byte[] bytes = wc.DownloadData("https://github.com/intiface/intiface-cli-rs/releases/latest/download/intiface-cli-rs-win-x64-Release.zip");
                    using var stream = new MemoryStream(bytes);

                    using var intifacestream = new ZipArchive(stream).GetEntry("IntifaceCLI.exe").Open();
                    Directory.CreateDirectory("Executables");
                    using var file = new FileStream("Executables/IntifaceCLI.exe", FileMode.Create, FileAccess.Write);
                    intifacestream.CopyTo(file);
                }
                catch (Exception e)
                {
                    Con.Error("Failed to download Buttplug CLI. If you start multiple instances of VRC this might occur");
                    Con.Error(e);
                }
            }

            buttplugCLIPath = "Executables/IntifaceCLI.exe";
        }

        private async Task ConnectButtplug()
        {
            try
            {
                if (buttplugClient != null)
                {
                    Con.Msg("Reconnecting to Buttplug");
                    await buttplugClient.DisconnectAsync();
                    buttplugClient = null;
                }
                buttplugClient = new ButtplugClient("Totally Wholesome");
                buttplugClient.DeviceAdded += ButtplugClient_DeviceAdded;
                buttplugClient.DeviceRemoved += ButtplugClient_DeviceRemoved;
                buttplugClient.ScanningFinished += async (_, _2) => await buttplugClient.StartScanningAsync();
                await buttplugClient.ConnectAsync(new ButtplugWebsocketConnectorOptions(new Uri($"ws://localhost:{port}")));
                await buttplugClient.StartScanningAsync();
                Con.Msg("Connection to Buttplug successful");
            }
            catch (Exception e)
            {
                Con.Error("Connection to Buttplug failed");
                Con.Error(e);
            }
        }

        [UIEventHandler("restartButtplug")]
        public static void RestartButtplug()
        {
            Instance.intifaceProcess?.Kill();
        }

        public void StartButtplugInstance()
        {
            if (Main.Instance.Quitting) return;
            try
            {

                Process found = null;
                foreach (var item in Process.GetProcesses())
                {
                    try
                    {
                        if (item.ProcessName == "IntifaceCLI")
                            found = item;
                    }
                    catch (Exception)
                    {
                        Con.Error("Error while retrieving Processname of running application");
                    }
                    
                } 
                if (found != null)
                {
                    Con.Msg("Intiface already running. Not starting again");
                    intifaceProcess = found;
                    intifaceProcess.EnableRaisingEvents = true;
                    intifaceProcess.Exited += (_, _2) => StartButtplugInstance();

                    new Task(async () => await ConnectButtplug()).Start();

                    return;
                }

                FileInfo target = new FileInfo(buttplugCLIPath);

                //TODO: Find a better way to handle Intiface, this is still scuffed... WHY CAN'T WE GET THE DATA AND HAVE IT WORK

                var startInfo = new ProcessStartInfo(target.FullName, $"--with-lovense-connect --wsinsecureport {port} --log error");
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                //startInfo.RedirectStandardError = true;
                //startInfo.RedirectStandardOutput = true;

                intifaceProcess = Process.Start(startInfo);
                intifaceProcess.EnableRaisingEvents = true;
                intifaceProcess.OutputDataReceived += (sender, args) => Con.Debug(args.Data);
                intifaceProcess.ErrorDataReceived += (sender, args) => Con.Error(args.Data);

                intifaceProcess.Exited += (_, _2) => StartButtplugInstance();

                new Task(async () => await ConnectButtplug()).Start();
            }
            catch (Exception ex)
            {
                //NotificationSystem.EnqueueNotification("Error", "Error starting intiface. Check log and try again", 5);
                Con.Error("Error starting intiface. Check log and try again");
                Con.Error(ex);
            }
        }

        internal void ShutDown()
        {
            Con.Msg("Shutting down Toy manager");

            //So it turns out that WebSocketSharp is dying(?) internally and causing our shutdown to not complete, disconnecting buttplug doesn't appear to be needed anyways?
            //Let's just let it die with VRChat so we can force Intiface to die. 

            if (intifaceProcess != null && !intifaceProcess.HasExited)
            {
                Con.Msg("Killing intiface");
                intifaceProcess?.Kill();
            }
        }

        private void ButtplugClient_DeviceRemoved(object sender, DeviceRemovedEventArgs e)
        {
            Con.Msg("Toy disconnected: " + e.Device.Name);
            NotificationSystem.EnqueueNotification("Toy removed", e.Device.Name, 3, null);
        }

        private void ButtplugClient_DeviceAdded(object sender, DeviceAddedEventArgs e)
        {
            Con.Msg("Toy connected: " + e.Device.Name);
            e.Device.SendStopDeviceCmd();
            NotificationSystem.EnqueueNotification("Toy added", e.Device.Name, 3);
        }

        public void ResetToys()
        {
            VibrateAtStrength(0);
        }

        public async void BeepBoop()
        {
            VibrateAtStrength(1);
            await Task.Delay(1000);
            VibrateAtStrength(0);
        }

        public void VibrateAtStrength(float strength)
        {
            if (VibeGoBrrCompatMode)
            {
                mIdleEnabled.SetValue(vgb, true);
                mIdleIntensity.SetValue(vgb, strength);
                return;
            }
            if (buttplugClient == null || !buttplugClient.Connected)
                return;
            foreach (var toy in buttplugClient.Devices)
            {
                if (toy.AllowedMessages.TryGetValue(DeviceMessages.VibrateCmd, out var messagedetails))
                    toy.SendVibrateCmd(new double[messagedetails.FeatureCount].Select(x => (double)strength));
                if (toy.AllowedMessages.TryGetValue(DeviceMessages.RotateCmd, out var rotateCmd))
                    toy.SendRotateCmd(new double[rotateCmd.FeatureCount].Select(x => ((double)strength, true)));
            }
        }

        private void OnMasterRemoteControlEvent(MasterRemoteControl packet)
        {
            if (!ConfigManager.Instance.IsActive(AccessType.EnableToyControl))
                return;
            if (!ConfigManager.Instance.IsActive(AccessType.AllowToyControl, LeadManager.Instance.MasterId))
                return;
            if (LeadManager.Instance.FollowerPair == null)
                return;
            
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!LeadManager.Instance.FollowerPair.AreWeFollower() || LeadManager.Instance.FollowerPair.Key != packet.Key)
                    return;
                
                VibrateAtStrength(packet.ToyStrength);

                if (buttplugClient == null || !buttplugClient.Connected)
                    return;
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadVibeGoBrrrIntegration()
        {
            Con.Msg("Starting VibeGoBrrr Integration");
            vgb = MelonMod.RegisteredMelons.FirstOrDefault(x => x.Info.Name == "VibeGoesBrrr");

            mIdleIntensity = vgb.GetType().GetField("mIdleIntensity", BindingFlags.Instance | BindingFlags.NonPublic);
            mIdleEnabled = vgb.GetType().GetField("mIdleEnabled", BindingFlags.Instance | BindingFlags.NonPublic);

            Con.Msg("Finished VibeGoBrrr Integration");
        }
        

        private bool CheckForIntifaceUpdate(WebClient client)
        {
            try
            {
                var request = client.DownloadString("https://api.github.com/repos/intiface/intiface-cli-rs/releases?per_page=1");
                var jsonArray = JsonConvert.DeserializeObject<JArray>(request);

                if (jsonArray != null)
                {
                    var entry = jsonArray.First;

                    if (entry is { HasValues: true })
                    {
                        var entryString = (string)entry["name"];
                        var result = !Configuration.JSONConfig.IntifaceReleaseVersion.Equals(entryString);

                        Con.Debug($"Intiface version is {entryString} | Outdated: {result}");

                        Configuration.JSONConfig.IntifaceReleaseVersion = entryString;
                        Configuration.SaveConfig();
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                Con.Error("An error occured while attempting to retrieve Intiface version info!");
                Con.Error(e);
            }

            return false;
        }
    }
}
