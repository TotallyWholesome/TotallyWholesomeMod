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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using ButtplugManaged;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.TWUI.Pages;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TotallyWholesome.Utils;
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
        private int port = 12345;
        private string extHost = null;
        private string buttplugCLIPath;

        private bool isUsingExternalInstance = false;

        private HashSet<LeadPair> _queue = new HashSet<LeadPair>();
        private Regex _portArgCheck = new Regex("--tw\\.buttplugport (?'bpport'\\d+)", RegexOptions.Compiled);
        private Regex _hostArgCheck = new Regex("--tw\\.buttplughost (?'bphost'\\S+)", RegexOptions.Compiled);

        //AdultToyAPI integration
        private MelonMod _adultToyAPI = null;
        private MethodInfo _getConnectedDevices = null;
        private MethodInfo _setMotorSpeed = null;

        //forallpets
        public SliderFloat ToyStrength;
        public SliderFloat ToyStrengthIPC;

        //Achievement data stuff
        public float ActiveVibrationStrength;

        public int ButtplugDeviceCount
        {
            get
            {
                if (_adultToyAPI != null) return GetDevices().Count;
                return buttplugClient != null ? buttplugClient.Devices.Length : 0;
            }
        }

        public int Priority => 1;

        public void Setup()
        {
            Instance = this;

            //ToChange
            if (!ConfigManager.Instance.IsActive(AccessType.EnableToyControl))
                return;

            TWNetListener.ButtplugUpdateEvent += OnButtplugUpdate;
            TWNetListener.LeadRemoveEvent += OnLeadRemoveEvent;

            _adultToyAPI = MelonMod.RegisteredMelons.FirstOrDefault(x => x.Info.Name.Equals("AdultToyAPI"));

            if (_adultToyAPI != null)
            {
                Con.Msg("Detected AdultToyAPI, starting integration!");
                LoadAdultToyAPIIntegration();
                return;
            }

            var match = _portArgCheck.Match(Environment.CommandLine);

            if (match.Success && int.TryParse(match.Groups["bpport"].Value, out var newPort))
            {
                Con.Msg($"Intiface Central/Engine port switched to {newPort}!");
                port = newPort;
            }

            match = _hostArgCheck.Match(Environment.CommandLine);

            if (match.Success)
            {
                extHost = match.Groups["bphost"].Value;
                Con.Msg($"Using external Intiface specifed by command line at {ButtplugUri}");
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

        public void LateSetup()
        {
            ToyStrength.OnValueUpdated += f => { TWNetSendHelpers.SendButtplugUpdate(); };

            ToyStrengthIPC.OnValueUpdated += f =>
            {
                var leadPair = IndividualPetControl.Instance.SelectedLeadPair;

                if (leadPair == null)
                    return;

                leadPair.ToyStrength = f;
                TWNetSendHelpers.SendButtplugUpdate(leadPair);
            };
        }

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

        public Uri ButtplugUri => new Uri($"ws://{extHost ?? "localhost"}:{port}");

        private async Task ConnectButtplug()
        {
            try
            {
                if (_adultToyAPI != null)
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
                await buttplugClient.ConnectAsync(new ButtplugWebsocketConnectorOptions(ButtplugUri));
                await buttplugClient.StartScanningAsync();
                Con.Msg("Connection to Buttplug successful");
            }
            catch (Exception e)
            {
                Con.Error("Connection to Buttplug failed");
                Con.Error(e);
            }
        }

        public static void RestartButtplug()
        {
            Instance.intifaceProcess?.Kill();
            Instance.StartButtplugInstance();
        }

        public void StartButtplugInstance()
        {
            if (_adultToyAPI != null)
            {
                return;
            }

            if (Main.Instance.Quitting) return;

            if (extHost != null)
            {
                Con.Msg("Intiface host specified via command line. Using external instance");
                isUsingExternalInstance = true;
                new Task(async () => await ConnectButtplug()).Start();
                return;
            }

            try
            {
                foreach (var item in Process.GetProcesses())
                {
                    try
                    {
                        //what todo in this case. Central is diffrent
                        if (item.ProcessName.Equals("intiface_central", StringComparison.InvariantCultureIgnoreCase))
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

                var startInfo = new ProcessStartInfo(target.FullName, $"--use-lovense-connect --use-bluetooth-le --use-lovense-dongle-hid --websocket-port {port}");
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                //startInfo.RedirectStandardError = true;
                //startInfo.RedirectStandardOutput = true;

                intifaceProcess = Process.Start(startInfo);
                intifaceProcess.EnableRaisingEvents = true;
                intifaceProcess.OutputDataReceived += (sender, args) => Con.Debug(args.Data);
                intifaceProcess.ErrorDataReceived += (sender, args) => Con.Error(args.Data);

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

        public void BeepBoop()
        {
            TwTask.Run(async () =>
            {
                VibrateAtStrength(1);
                await Task.Delay(1000);
                VibrateAtStrength(0);
            });
        }

        private List<object> GetDevices()
        {
            if (_adultToyAPI == null) return null;

            IEnumerable temp = _getConnectedDevices.Invoke(_adultToyAPI, new object[] { }) as IEnumerable;

            return temp?.Cast<object>().ToList();
        }

        public void VibrateAtStrength(float strength)
        {
            if (_adultToyAPI != null)
            {
                var devices = GetDevices();
                foreach (var toy in devices)
                {
                    _setMotorSpeed.Invoke(_adultToyAPI, new[] { toy, 0, strength });
                }

                return;
            }

            if (buttplugClient is not { Connected: true })
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
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadAdultToyAPIIntegration()
        {
            Con.Debug("Attempting to fetch AdultToyAPI methods...");

            _getConnectedDevices = _adultToyAPI.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => string.Equals(m.Name, "GetConnectedDevices"));
            _setMotorSpeed = _adultToyAPI.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(m => string.Equals(m.Name, "SetMotorSpeed"));

            if (_getConnectedDevices == null || _setMotorSpeed == null)
            {
                Con.Error("Unable to fetch methods for AdultToyAPI! TW will not be able to use AdultToyAPI right now!");
                Con.Error($"GetConnectedDevices: {_getConnectedDevices != null} | SetMotorSpeed: {_setMotorSpeed != null}");

                //Set the MelonMod reference to null to prevent other functions from trying to use null methodinfos
                _adultToyAPI = null;
            }

            Con.Msg("AdultToyAPI is ready for use!");
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