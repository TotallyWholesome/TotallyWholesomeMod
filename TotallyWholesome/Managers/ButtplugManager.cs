using System;
using System.Collections;
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
using TWNetCommon.Data.ControlPackets;
using WholesomeLoader;

namespace TotallyWholesome.Managers
{
    public class ButtplugManager : ITWManager
    {

        public static ButtplugManager Instance;
        public ButtplugClient buttplugClient;

        public Action ButtplugDeviceAdded;
        public Action ButtplugDeviceRemoved;
        
        private Process intifaceProcess;
        private int port = 12344;
        private string buttplugCLIPath;

        private bool isUsingExternalInstance = false;

        private HashSet<LeadPair> queue = new HashSet<LeadPair>();

        //AdultToyAPI integration
        private MelonMod AdultToyAPI = null;
        private MethodInfo GetConnectedDevices = null;
        private MethodInfo SetMotorSpeed = null;

        //forallpets
        public SliderFloat ToyStrength;
        public SliderFloat ToyStrengthIPC;
        
        //Achievement data stuff
        public float ActiveVibrationStrength;
        public int ButtplugDeviceCount
        {
            get
            {
                if (buttplugClient == null) return 0;
                if(AdultToyAPI!=null)
                {
                    return GetDevices().Count();
                }
                return buttplugClient.Devices.Length;
            }
        }

        public string ManagerName() => nameof(ButtplugManager);
        public int Priority() => 1;
        public void Setup()
        {
            Instance = this;

            ToyStrength = new SliderFloat("lovenseStrengthSlider", 0f);
            ToyStrength.OnValueUpdated += f => { TWNetSendHelpers.SendButtplugUpdate(); };

            ToyStrengthIPC = new SliderFloat("lovenseStrengthSliderIPC", 0f);
            ToyStrengthIPC.OnValueUpdated += f =>
            {
                var leadPair = UserInterface.Instance.SelectedLeadPair;

                if (leadPair == null)
                    return;

                leadPair.ToyStrength = f;
                TWNetSendHelpers.SendButtplugUpdate(leadPair);
            };


            //ToChange
            if (!ConfigManager.Instance.IsActive(AccessType.EnableToyControl))
                return;

            TWNetListener.ButtplugUpdateEvent += OnButtplugUpdate;
            TWNetListener.LeadRemoveEvent += OnLeadRemoveEvent;

            foreach (var melon in MelonMod.RegisteredMelons)
            {
                Con.Msg($"Detected mods: {melon.Info.Name}");
                if (string.Equals(melon.Info.Name, "AdultToyAPI"))
                {
                    Con.Msg($"Found Adult Toy API");
                    AdultToyAPI = melon;
                }
            }

            if (AdultToyAPI!=null)
            {
                LoadAdultToyAPIIntegration();
                return;
            }

            DownloadButtplugCLI();


            try
            {
                StartButtplugInstance();
            }
            catch (Exception e)
            {
                Con.Error("Failed to start Buttplug Engine");
                Con.Error(e);
            }
        }

        public void LateSetup() { }

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

            if (CheckForIntifaceUpdate(wc) || !File.Exists("Executables/intiface-engine.exe"))
            {
                Con.Msg("New Intiface version detected! Downloading to Executables/intiface-engine.exe!");

                try
                {
                    byte[] bytes = wc.DownloadData("https://github.com/intiface/intiface-engine/releases/latest/download/intiface-engine-win-x64-Release.zip");
                    using var stream = new MemoryStream(bytes);

                    using var intifacestream = new ZipArchive(stream).GetEntry("intiface-engine.exe").Open();
                    Directory.CreateDirectory("Executables");
                    using var file = new FileStream("Executables/intiface-engine.exe", FileMode.Create, FileAccess.Write);
                    intifacestream.CopyTo(file);
                }
                catch (Exception e)
                {
                    Con.Error("Failed to download Buttplug Engine. If you start multiple instances of VRC this might occur");
                    Con.Error(e);
                }
            }

            buttplugCLIPath = "Executables/intiface-engine.exe";
        }

        private async Task ConnectButtplug()
        {
            try
            {
                if(AdultToyAPI!=null)
                {
                    return;
                }
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
            if (Instance.isUsingExternalInstance)
                Instance.StartButtplugInstance();
        }

        public void StartButtplugInstance()
        {
            if(AdultToyAPI!=null)
            { return; }
            if (Main.Instance.Quitting) return;
            try
            {
                foreach (var item in Process.GetProcesses())
                {
                    try
                    {
                        //what todo in this case. Central is diffrent
                        if (item.ProcessName == "intiface_central.exe")
                        {
                            Con.Msg("Intiface Central running. Using running instance");
                            isUsingExternalInstance = true;
                            new Task(async () => await ConnectButtplug()).Start();
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        Con.Error("Error while retrieving Processname of running application");
                    }

                }

                FileInfo target = new FileInfo(buttplugCLIPath);

                //TODO: Find a better way to handle Intiface, this is still scuffed... WHY CAN'T WE GET THE DATA AND HAVE IT WORK

                var startInfo = new ProcessStartInfo(target.FullName, $"--use-lovense-connect --use-bluetooth-le --websocket-port {port}");
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
                Con.Error("Error starting intiface engine. Check log and try again");
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
            ButtplugDeviceRemoved?.Invoke();
            NotificationSystem.EnqueueNotification("Toy removed", e.Device.Name, 3, null);
        }

        private void ButtplugClient_DeviceAdded(object sender, DeviceAddedEventArgs e)
        {
            Con.Msg("Toy connected: " + e.Device.Name);
            e.Device.SendStopDeviceCmd();
            ButtplugDeviceAdded?.Invoke();
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
        private List<object> GetDevices()
        {
            Con.Msg($"GetDevices");
            if(GetConnectedDevices!=null && AdultToyAPI!=null)
            {
                Con.Msg($"Get Devices from adult toy api");
                IEnumerable temp = GetConnectedDevices.Invoke(AdultToyAPI, new object[] { }) as IEnumerable;
                List<object> result = new List<object>();
                foreach(var obj in temp)
                { 
                    result.Add(obj);
                }
                return result;
            }
            return new List<object>();
        }
        public void VibrateAtStrength(float strength)
        {
            Con.Msg($"Vibrate at strength: {strength}");
            if (AdultToyAPI!=null)
            {
                Con.Msg($"Adult Toy API found");
                List<object> devices = GetDevices();
                foreach (object toy in devices)
                {
                    Con.Msg($"toy found");
                    if (SetMotorSpeed != null)
                    {
                        SetMotorSpeed.Invoke(AdultToyAPI, new object[] { toy, 0, strength });
                    }
                }
                return;
            }
            if (buttplugClient == null || !buttplugClient.Connected)
                return;
            ActiveVibrationStrength = strength;
            foreach (var toy in buttplugClient.Devices)
            {
                if (toy.AllowedMessages.TryGetValue(DeviceMessages.VibrateCmd, out var messagedetails))
                    toy.SendVibrateCmd(new double[messagedetails.FeatureCount].Select(x => (double)strength));
                if (toy.AllowedMessages.TryGetValue(DeviceMessages.RotateCmd, out var rotateCmd))
                    toy.SendRotateCmd(new double[rotateCmd.FeatureCount].Select(x => ((double)strength, true)));
            }
        }

        private void OnButtplugUpdate(ButtplugUpdate packet)
        {
            if (!ConfigManager.Instance.IsActive(AccessType.EnableToyControl))
                return;
            if (!ConfigManager.Instance.IsActive(AccessType.AllowToyControl, LeadManager.Instance.MasterId))
                return;
            if (LeadManager.Instance.MasterPair == null)
                return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!LeadManager.Instance.MasterPair.AreWeFollower() || LeadManager.Instance.MasterPair.Key != packet.Key)
                    return;

                VibrateAtStrength(packet.ToyStrength);

                if (buttplugClient == null || !buttplugClient.Connected)
                    return;
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadAdultToyAPIIntegration()
        {
            Con.Msg("Starting AdultToyAPI Integration");

            GetConnectedDevices = AdultToyAPI.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(m=>string.Equals(m.Name, "GetConnectedDevices"));
            SetMotorSpeed = AdultToyAPI.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(m => string.Equals(m.Name, "SetMotorSpeed"));

            Con.Msg("Finished VibeGoBrrr Integration");
        }


        private bool CheckForIntifaceUpdate(WebClient client)
        {
            try
            {
                var request = client.DownloadString("https://api.github.com/repos/intiface/intiface-engine/releases?per_page=1");
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
