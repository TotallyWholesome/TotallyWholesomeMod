using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Savior;
using cohtml;
using MelonLoader;
using Newtonsoft.Json;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Network;
using TotallyWholesome.Objects;
using TotallyWholesome.Objects.ConfigObjects;
using TotallyWholesome.TWUI;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using UnityEngine;
using WholesomeLoader;
using Yggdrasil.Extensions;

namespace TotallyWholesome.Managers
{
    public class PiShockManager : ITWManager
    {
        public static PiShockManager Instance;

        private string PiShockName => "TotallyWholesome";
        private string PiShockApiOperate => "https://do.pishock.com/api/shortapiOperate";
        private string PiShockApiInfo => "https://do.pishock.com/api/getshockerinfoshort";
        private string PiShockGetKey => "https://do.pishock.com/api/GetKeyAndNameFromShort";
        private static string PiShockGetLogs => "https://do.pishock.com/api/getlastlogsfromshort";

        private static readonly HttpClient httpClient = new HttpClient();

        //for all pets
        public SliderFloat Strength;
        public SliderFloat Duration;
        public ShockOperation Operation = 0;
        public bool ShockHeightEnabled = false;
        public SliderFloat ShockHeight;
        public SliderFloat ShockHeightStrengthMin;
        public SliderFloat ShockHeightStrengthMax;
        public SliderFloat ShockHeightStrengthStep;
        
        //Individual Pet Controls
        public SliderFloat StrengthIPC;
        public SliderFloat DurationIPC;
        public SliderFloat ShockHeightIPC;
        public SliderFloat ShockHeightStrengthMinIPC;
        public SliderFloat ShockHeightStrengthMaxIPC;
        public SliderFloat ShockHeightStrengthStepIPC;
        
        //Achievement data stuff
        public ShockOperation LastOperationPet;
        public int LastStrengthPet;
        public int LastDurationPet;
        public DateTime LastOperationFiredPet;
        public ShockOperation LastOperationMaster;
        public int LastStrengthMaster;
        public int LastDurationMaster;
        public DateTime LastOperationFiredMaster;
        public DateTime LastOperationGlobalMaster;
        public PiShockerInfo LastShockerInfo;
        public Action PiShockDeviceUpdated;

        public string ManagerName() => nameof(PiShockManager);

        private Timer cubeTimer;
        private GameObject cubeShow;
        private object _piShockHeightCoroutineToken;

        internal PiShockUpdate lastPacket;

        private int heightState = -1;
        
        private static DateTime _lastRefresh;
        private static DateTime _lastLogsRefresh;

        public static Color ColorShown = new Color(0, 1, 0, 0.3f);
        private static Color ColorRed = new Color(1, 0, 0, 1f);
        private static Color ColorHidden = new Color(0, 1, 0, 0);

        public void Setup()
        {
            Instance = this;

            Strength = new SliderFloat("piShockStrengthSlider", 0f);
            Duration = new SliderFloat("piShockDurationSlider", 0f);
            ShockHeight = new SliderFloat("piShockHeightSlider", 0f);
            ShockHeight.OnValueUpdated += _ => UpdateShockHeightControl();
            ShockHeightStrengthMin = new SliderFloat("piShockMinStrengthSlider", 0f);
            ShockHeightStrengthMin.OnValueUpdated += _ => UpdateShockHeightControl();
            ShockHeightStrengthMax = new SliderFloat("piShockMaxStrengthSlider", 0f);
            ShockHeightStrengthMax.OnValueUpdated += _ => UpdateShockHeightControl();
            ShockHeightStrengthStep = new SliderFloat("piShockStepStrengthSlider", 0f);
            ShockHeightStrengthStep.OnValueUpdated += _ => UpdateShockHeightControl();

            StrengthIPC = new SliderFloat("piShockStrengthSliderIPC", 0f);
            StrengthIPC.OnValueUpdated += f =>
            {
                var leadPair = UserInterface.Instance.SelectedLeadPair;
                
                if (leadPair == null)
                    return;
                
                leadPair.ShockStrength = (int)Math.Round(f);
            };
            DurationIPC = new SliderFloat("piShockDurationSliderIPC", 0f);
            DurationIPC.OnValueUpdated += f =>
            {
                var leadPair = UserInterface.Instance.SelectedLeadPair;
                
                if (leadPair == null)
                    return;
                
                leadPair.ShockDuration = (int)Math.Round(f);
            };
            ShockHeightIPC = new SliderFloat("piShockHeightSliderIPC", 0f);
            ShockHeightIPC.OnValueUpdated += f => SetShockHeightControlIPC(height: f);
            ShockHeightStrengthMinIPC = new SliderFloat("piShockMinStrengthSliderIPC", 0f);
            ShockHeightStrengthMinIPC.OnValueUpdated += f => SetShockHeightControlIPC(min: f);
            ShockHeightStrengthMaxIPC = new SliderFloat("piShockMaxStrengthSliderIPC", 0f);
            ShockHeightStrengthMaxIPC.OnValueUpdated += f => SetShockHeightControlIPC(max: f);
            ShockHeightStrengthStepIPC = new SliderFloat("piShockStepStrengthSliderIPC", 0f);
            ShockHeightStrengthStepIPC.OnValueUpdated += f => SetShockHeightControlIPC(step: f);

            TWNetListener.PiShockUpdateEvent += ReceivePiShockEvent;
            LeadManager.OnLeadPairDestroyed += OnLeadRemoveEvent;
            UserInterface.Instance.OnOpenedPage += OnOpenedPage;
        }

        public void LateSetup()
        {
            if(cubeShow == null)
                cubeShow = CreateCube();
        }

        public int Priority() => 1;

        #region UI Actions

        public void ChangeShockerState(string key, bool state)
        {
            var shocker = Configuration.JSONConfig.PiShockShockers.FirstOrDefault(x => x.Key.Equals(key));

            if (shocker == null)
            {
                OnOpenedPage("ShockerManagement", null);
                return;
            }

            shocker.Enabled = state;
            
            PiShockDeviceUpdated?.Invoke();
        }

        private static string lastShockerId = "";
        
        public static void OnShockerAction(string shockerID, string action)
        {
            var shocker = Configuration.JSONConfig.PiShockShockers.FirstOrDefault(x => x.Key.Equals(shockerID));

            if (shocker == null)
            {
                Instance.OnOpenedPage("ShockerManagement", null);
                return;
            }

            switch (action)
            {
                case "SetPriority":
                    Con.Debug($"Setting prioritized for {shocker.Key}");
                    foreach (var shockerConfig in Configuration.JSONConfig.PiShockShockers)
                    {
                        shockerConfig.Prioritized = shockerConfig.Equals(shocker);
                        Instance.OnOpenedPage("ShockerManagement", null);
                    }
                    Configuration.SaveConfig();
                    break;
                case "RemoveShocker":
                    Con.Debug($"Showing confirm for remove on {shocker.Key}");
                    UIUtils.ShowConfirm("Remove Shocker?", $"Are you sure you want to remove the shocker named \"{shocker.Name}\"?", "Yes", () =>
                    {
                        Configuration.JSONConfig.PiShockShockers.Remove(shocker);
                        Configuration.SaveConfig();
                        Instance.PiShockDeviceUpdated?.Invoke();
                        Instance.OnOpenedPage("ShockerManagement", null);
                    });
                    break;
                case "OpenLogs":
                    Con.Debug("Opening logs page");
                    TWUtils.GetInternalView().TriggerEvent("twOpenShockerLogs", shocker.Name);
                    lastShockerId = shocker.Key;
                    OnShockerAction(shocker.Key, "GetLogs");
                    break;
                case "GetLogs":
                    Con.Debug($"Getting logs for shocker with name: {shocker.Name}");
                    PiShockerLog[] logs;
                    var task = new Task(async () =>
                    {
                        try
                        {
                            logs = await GetShockerLog(shocker.Key);
                            Main.Instance.MainThreadQueue.Enqueue(() => TWUtils.GetInternalView().TriggerEvent("twUpdateLogPanel", logs));
                        }
                        catch (Exception)
                        {
                            Con.Warn($"Error has occured while getting logs for shocker with the name: {shocker.Name}");

                        }
                    });
                    task.Start();
                    break;
            }
        }

        [UIEventHandler("pishockLogsRefresh")]
        public static void LogsRefresh()
        {
            if (DateTime.Now.Subtract(_lastLogsRefresh).TotalSeconds < 10)
            {
                UIUtils.ShowNotice("Hold up!", "You've already refreshed your shocker logs! Please wait a bit before refreshing again!");
                return;
            }
            
            _lastLogsRefresh = DateTime.Now;
            
            OnShockerAction(lastShockerId, "GetLogs");
        }

        [UIEventHandler("pishockBeep")]
        public static void BeepAction()
        {
            Instance.Operation = ShockOperation.Beep;
            TWNetSendHelpers.SendPiShockUpdate();
            
            UIUtils.ShowToast($"Sent PiShock Beep to all pets for {Instance.Duration.SliderValue} seconds!");
        }

        [UIEventHandler("pishockVibrate")]
        public static void VibrateAction()
        {
            Instance.Operation = ShockOperation.Vibrate;
            TWNetSendHelpers.SendPiShockUpdate();
            
            UIUtils.ShowToast($"Sent PiShock Vibrate to all pets for {Instance.Duration.SliderValue} seconds at {Instance.Strength.SliderValue}%!");
        }

        [UIEventHandler("pishockShock")]
        public static void ShockAction()
        {
            Instance.Operation = ShockOperation.Shock;
            TWNetSendHelpers.SendPiShockUpdate();
            
            UIUtils.ShowToast($"Sent PiShock Shock to all pets for {Instance.Duration.SliderValue} seconds at {Instance.Strength.SliderValue}%!");
        }
        
        [UIEventHandler("pishockBeepIPC")]
        public static void BeepActionIPC()
        {
            var leadPair = UserInterface.Instance.SelectedLeadPair;
                
            if (leadPair == null)
                return;
            
            leadPair.ShockOperation = ShockOperation.Beep;
            TWNetSendHelpers.SendPiShockUpdate(leadPair);
            
            UIUtils.ShowToast($"Sent PiShock Beep to {leadPair.Pet.Username} for {leadPair.ShockDuration} seconds!");
        }

        [UIEventHandler("pishockVibrateIPC")]
        public static void VibrateActionIPC()
        {
            var leadPair = UserInterface.Instance.SelectedLeadPair;
                
            if (leadPair == null)
                return;
            
            leadPair.ShockOperation = ShockOperation.Vibrate;
            TWNetSendHelpers.SendPiShockUpdate(leadPair);
            
            UIUtils.ShowToast($"Sent PiShock Vibrate to {leadPair.Pet.Username} for {leadPair.ShockDuration} seconds at {leadPair.ShockStrength}%!");
        }

        [UIEventHandler("pishockShockIPC")]
        public static void ShockActionIPC()
        {
            var leadPair = UserInterface.Instance.SelectedLeadPair;
                
            if (leadPair == null)
                return;
            
            leadPair.ShockOperation = ShockOperation.Shock;
            TWNetSendHelpers.SendPiShockUpdate(leadPair);
            
            UIUtils.ShowToast($"Sent PiShock Shock to {leadPair.Pet.Username} for {leadPair.ShockDuration} seconds at {leadPair.ShockStrength}%!");
        }

        [UIEventHandler("AddShocker")]
        public static void AddShocker()
        {
            TWUtils.OpenKeyboard("Enter your PiShock Share Code", s =>
            {
                Con.Debug($"Attempting to add PiShock Shocker using Share Code {s}");
                Instance.RegisterNewToken(s, (s1, s2) =>
                {
                    UIUtils.ShowNotice("Success!", "Successfully added a new PiShock Shocker!");
                    Instance.OnOpenedPage("ShockerManagement", null);
                }, () =>
                {
                    UIUtils.ShowNotice("Failed", "Failed to add PiShock Shocker! Please check that the share code is valid, if you are continuing to have issues please check the MelonLoader Console!");
                });
            });
        }

        [UIEventHandler("RefreshShockerInfo")]
        public static void RefreshShockerInfo()
        {
            if (DateTime.Now.Subtract(_lastRefresh).TotalSeconds < 30)
            {
                UIUtils.ShowNotice("Hold up!", "You've already refreshed your shockers! Please wait a bit before refreshing again!");
                return;
            }
            
            _lastRefresh = DateTime.Now;
            
            Instance.RefreshShockerInfo(() =>
            {
                Instance.OnOpenedPage("ShockerManagement", null);
            });
        }
        
        private void OnOpenedPage(string arg1, string arg2)
        {
            if (!arg1.Equals("ShockerManagement")) return;
            
            TWUtils.GetInternalView().TriggerEvent("twUpdateShockerManagement", Configuration.JSONConfig.PiShockShockers);
        }

        public static void UpdateShockHeightControl(bool? enable = null)
        {
            if (enable != null)
                Instance.ShockHeightEnabled = enable.Value;

            if (Instance != null && TWUtils.GetOurPlayer() != null)
                Instance.ShowCube(TWUtils.GetOurPlayer(), Instance.ShockHeight.SliderValue, true, ColorShown);
            TWNetSendHelpers.SendPiShockUpdate();
        }
        
        public static void SetShockHeightControlIPC(bool? enable = null, float? height = null, float? min = null, float? max = null, float? step = null)
        {
            var leadPair = UserInterface.Instance.SelectedLeadPair;
                
            if (leadPair == null)
                return;

            if (enable != null)
                leadPair.ShockHeightEnabled = enable.Value;
            if (height != null)
                leadPair.ShockHeight = height.Value;
            if (min != null)
                leadPair.ShockHeightStrengthMin = min.Value;
            if (max != null)
                leadPair.ShockHeightStrengthMax = max.Value;
            if (step != null)
                leadPair.ShockHeightStrengthStep = step.Value;
            
            if (Instance != null)
                Instance.ShowCube(leadPair.Pet, leadPair.ShockHeight, true, ColorShown);
            TWNetSendHelpers.SendPiShockUpdate(leadPair);
        }

        #endregion

        public void BeepBoop()
        {
            Execute(ShockOperation.Vibrate, 1, 100);
        }

        public void Reset()
        {
            lastPacket = null;
            cubeShow.SetActive(false);
            cubeShow.GetComponent<MeshRenderer>().material.color = ColorHidden;
        }

        public void RegisterNewToken(string code, Action<string, string> onCompleted, Action onFailed)
        {
            Con.Debug(code);
            var task = new Task(async () =>
            {
                var keyRequest = new PiShockGetToken()
                {
                    Name = PiShockName,
                    Code = code
                };
                var keyRequesthJson = JsonConvert.SerializeObject(keyRequest, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var response = await httpClient.PostAsync(PiShockGetKey, new StringContent(keyRequesthJson, Encoding.UTF8, "application/json"));

                Con.Debug(response);
                try
                {
                    response.EnsureSuccessStatusCode();
                    var key = JsonConvert.DeserializeObject<PiShockShortCodeResp>(await response.Content.ReadAsStringAsync());

                    if (key == null)
                    {
                        Con.Error("Failed to register shocker! Could not deserialize response!");
                        Main.Instance.MainThreadQueue.Enqueue(() => onFailed?.Invoke());
                        return;
                    }

                    Con.Debug($"Got PiShockShortCodeResp: {key.Key}|{key.ShockerName}");

                    Configuration.JSONConfig.PiShockShockers.Add(new PiShockShocker(key.Key, key.ShockerName));
                    Configuration.SaveConfig();
                    Con.Debug("Saved key");
                    PiShockDeviceUpdated?.Invoke();

                    //Successful
                    Main.Instance.MainThreadQueue.Enqueue(() => onCompleted?.Invoke(key.Key, key.ShockerName));
                }
                catch (Exception e)
                {
                    Con.Error("An error occured or that PiShock short code was invalid!");
                    Con.Error(e);

                    Main.Instance.MainThreadQueue.Enqueue(() => onFailed?.Invoke());
                }
            });

            task.Start();
        }

        /// <summary>
        /// Refreshes the info for all PiShock Shockers
        /// </summary>
        /// <param name="onComplete">Action to be fired from MainThreadQueue</param>
        public void RefreshShockerInfo(Action onComplete)
        {
            var task = new Task(async () =>
            {
                foreach (var shocker in Configuration.JSONConfig.PiShockShockers)
                {
                    try
                    {
                        var shockerInfo = await GetShockerInfo(shocker.Key);

                        shocker.Name = shockerInfo.Name;
                    }
                    catch (Exception)
                    {
                        Con.Warn($"It appears that shocker {shocker.Name} may not be usable anymore. If the share code has been deleted you might wanna delete this shocker!");
                    }
                }

                Configuration.SaveConfig();
                Main.Instance.MainThreadQueue.Enqueue(() => onComplete?.Invoke());
            });
            task.Start();
        }

        private void Execute(ShockOperation op, int duration, int strength)
        {
            Con.Debug($"PiShockManager.Execute  [{op}|{duration}|{strength}]");

            if ((op == ShockOperation.Beep && !ConfigManager.Instance.IsActive(AccessType.AllowBeep, LeadManager.Instance.MasterId)) ||
                (op == ShockOperation.Vibrate && !ConfigManager.Instance.IsActive(AccessType.AllowVibrate, LeadManager.Instance.MasterId)) ||
                (op == ShockOperation.Shock && !ConfigManager.Instance.IsActive(AccessType.AllowShock, LeadManager.Instance.MasterId)))

            {
                Con.Debug($"PiShockManager.Execute not allowed");
                return;
            }
            if (!Configuration.JSONConfig.PiShockShockers.Any(x => x.Enabled)) return;
            PiShockShocker shocker;

            if (ConfigManager.Instance.IsActive(AccessType.PiShockRandomShocker, LeadManager.Instance.MasterId))
            {
                shocker = Configuration.JSONConfig.PiShockShockers.Where(x => x.Enabled).Random();
            }
            else
            {
                shocker = Configuration.JSONConfig.PiShockShockers.FirstOrDefault(x => x.Prioritized && x.Enabled) ?? Configuration.JSONConfig.PiShockShockers.FirstOrDefault(x => x.Enabled);
            }

            Task.Run(async () =>
            {
                try
                {
                    var shockerInfo = await GetShockerInfo(shocker.Key);
                    if (shockerInfo == null)
                        return;
                    
                    var command = new PiShockJsonCommand()
                    {
                        Apikey = shocker.Key,
                        Name = LeadManager.Instance?.MasterPair?.Master?.Username ?? PiShockName,
                        Op = op
                    };

                    LastOperationPet = op;
                    LastOperationFiredPet = DateTime.Now;

                    command.Duration = (int)Math.Ceiling(duration / 15f * shockerInfo.MaxDuration);
                    LastDurationPet = command.Duration.Value;
                    if (op == ShockOperation.Shock || op == ShockOperation.Vibrate)
                    {
                        command.Intensity = (int)Math.Ceiling(strength / 100f * shockerInfo.MaxIntensity);
                        LastStrengthPet = command.Intensity.Value;
                    }
                    var commandsJson = JsonConvert.SerializeObject(command, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    
                    if(op == ShockOperation.Shock)
                        AvatarParameterManager.Instance.TrySetTemporaryParameter("TWShockerShock", 1f, 0f, command.Duration.Value, 1.5f); //Wait 1.5 seconds before applying? Is that enough for fedacks api?

                    await httpClient.PostAsync(PiShockApiOperate, new StringContent(commandsJson, Encoding.UTF8, "application/json"));
                }
                catch (Exception ex)
                {
                    Con.Warn("Error occured while communicating with PiShock");
                    Con.Warn(ex);
                }
            });
            
        }

        private async Task<PiShockerInfo> GetShockerInfo(string key)
        {
            try
            {
                var auth = new PiShockJsonAuth()
                {
                    Apikey = key,
                };

                var authJson = JsonConvert.SerializeObject(auth, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var response = await httpClient.PostAsync(PiShockApiInfo, new StringContent(authJson, Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
                
                var info = JsonConvert.DeserializeObject<PiShockerInfo>(await response.Content.ReadAsStringAsync());

                if (LastShockerInfo != null)
                {
                    if (Monitor.TryEnter(LastShockerInfo, 0))
                    {
                        LastShockerInfo = info;
                        Monitor.Exit(LastShockerInfo);
                    }
                }

                return info;
            }
            catch (Exception e)
            {
                Con.Warn("A problem occured while trying to retrieve shocker info!");
                Con.Warn(e);
            }

            return null;
        }
        
        private static async Task<PiShockerLog[]> GetShockerLog(string code)
        {
            try
            {
                var auth = new PiShockJsonLog()
                {
                    Code = code,
                };

                var authJson = JsonConvert.SerializeObject(auth, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var response = await httpClient.PostAsync(PiShockGetLogs, new StringContent(authJson, Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();

                var a = await response.Content.ReadAsStringAsync();
                //Con.Msg(a);

                return JsonConvert.DeserializeObject<PiShockerLog[]>(a);
            }
            catch (Exception e)
            {
                Con.Error("A problem occured while trying to retrieve shocker logs!");
                Con.Error(e);
            }

            return null;
        }

        private void OnLeadRemoveEvent(LeadPair pair)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!pair.AreWeFollower())
                    return;

                Reset();
            });
        }

        private void ReceivePiShockEvent(PiShockUpdate packet)
        {
            if (LeadManager.Instance.MasterPair == null) return;
            if (LeadManager.Instance.MasterPair.Key != packet.Key) return;
            if (!ConfigManager.Instance.IsActive(AccessType.AllowShockControl, LeadManager.Instance.MasterId)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!LeadManager.Instance.MasterPair.AreWeFollower()) return;

                if (ConfigManager.Instance.IsActive(AccessType.AllowHeightControl, LeadManager.Instance.MasterId))
                    lastPacket = packet;
                if (packet.ShockHeightEnabled)
                    ShowCube(TWUtils.GetOurPlayer(), packet.ShockHeight, false);
                else
                    ShowCube(TWUtils.GetOurPlayer(), 9999, true);

                if ((packet?.ShockOperation ?? ShockOperation.NoOp) == ShockOperation.NoOp)
                    return;

                Execute(packet.ShockOperation, packet.ShockDuration, packet.ShockStrength);
            });
        }

        public void ShowCube(TWPlayerObject player, float height, bool hideAgain, Color? color = null)
        {
            if (cubeShow == null)
                cubeShow = CreateCube();
            
            if (!color.HasValue)
                color = ColorShown;

            cubeShow.SetActive(true);
            
            cubeShow.transform.position = GetMinPos(player) + new Vector3(0, height, 0);
            cubeShow.GetComponent<MeshRenderer>().material.color = color.Value;
            if (cubeTimer == null)
            {
                cubeTimer = new Timer(
                    async (_) =>
                    {
                        await Task.Delay(1000);
                        Main.Instance.MainThreadQueue.Enqueue(() =>
                        {
                            cubeShow.SetActive(false);
                            cubeShow.GetComponent<MeshRenderer>().material.color = ColorHidden;
                        });
                    }
                );
            }

            if (hideAgain)
            {
                cubeTimer.Change(5000, Timeout.Infinite);

                if (_piShockHeightCoroutineToken == null) return;
                Con.Debug("Stopping PiShock Height Coroutine");
                MelonCoroutines.Stop(_piShockHeightCoroutineToken);
                _piShockHeightCoroutineToken = null;
            }
            else
            {
                if (_piShockHeightCoroutineToken != null) return;
                Con.Debug("Starting PiShock Height Coroutine");
                _piShockHeightCoroutineToken = MelonCoroutines.Start(PiShockHeightCoroutine());
            }

        }

        private int ticks = 0;

        private IEnumerator PiShockHeightCoroutine()
        {
            while (!Main.Instance.Quitting)
            {
                yield return new WaitForSeconds(1);

                if (!ConfigManager.Instance.IsActive(AccessType.AllowHeightControl, LeadManager.Instance.MasterId) || LeadManager.Instance.MasterPair == null)
                    continue;

                if (!LeadManager.Instance.MasterPair.AreWeFollower())
                    continue;

                if (cubeShow == null || lastPacket == null || !lastPacket.ShockHeightEnabled)
                    continue;

                var height = HeadHeightFromBase();

                Con.Debug("PiShock Coroutine Checks Cleared");

                ShowCube(TWUtils.GetOurPlayer(), lastPacket.ShockHeight, false, height > lastPacket.ShockHeight ? ColorRed : ColorShown);

                ticks = (ticks + 1) % 5;
                if (ticks != 0)
                    continue;

                Con.Debug("It is time to run the checks, we've hit the time!");

                if (height > lastPacket.ShockHeight)
                {
                    heightState++;

                    if (heightState == 1 && ConfigManager.Instance.IsActive(AccessType.HeightControlWarning, LeadManager.Instance.MasterId))
                    {
                        Execute(ShockOperation.Vibrate, 1, 100);
                    }
                    else if (heightState > 1 || (ConfigManager.Instance.IsActive(AccessType.HeightControlWarning, LeadManager.Instance.MasterId) == false && heightState > 0))
                    {
                        var shockLevel = lastPacket.ShockHeightStrengthMin + ((heightState - 1) * lastPacket.ShockHeightStrengthStep);
                        if (shockLevel > lastPacket.ShockHeightStrengthMax)
                            shockLevel = lastPacket.ShockHeightStrengthMax;
                        Execute(ShockOperation.Shock, 1, (int)Math.Ceiling(shockLevel * 100));
                    }
                }
                else
                {
                    heightState--;
                }
                heightState = Math.Max(heightState, 0);
            }
        }

        private float HeadHeightFromBase()
        {
            return HeadHeight() - BaseHeight();
        }

        private float BaseHeight(TWPlayerObject player = null)
        {
            return GetMinPos(player).y;
        }

        private Vector3 GetMinPos(TWPlayerObject player = null)
        {
            if (player == null)
                player = TWUtils.GetOurPlayer();
            var animator = player.AvatarAnimator;
            if (animator == null)
                return Vector3.zero;

            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot).transform.position;
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot).transform.position;

            if (leftFoot.y < rightFoot.y)
                return leftFoot;
            return rightFoot;

        }



        private float HeadHeight(TWPlayerObject player = null)
        {
            player ??= TWUtils.GetOurPlayer();

            return player.AvatarAnimator != null ? player.AvatarAnimator.GetBoneTransform(HumanBodyBones.Head).transform.position.y : 0;
        }

        private GameObject CreateCube()
        {
            if (cubeShow != null)
                return cubeShow;
            
            Vector3[] vertices = {
                new Vector3 (-1, -1, -1),
                new Vector3 (1, -1, -1),
                new Vector3 (1, 1, -1),
                new Vector3 (-1, 1, -1),
                new Vector3 (-1, 1, 1),
                new Vector3 (1, 1, 1),
                new Vector3 (1, -1, 1),
                new Vector3 (-1, -1, 1),
            };

            int[] triangles = {
                0, 2, 1, //face front
			    0, 3, 2,
                2, 3, 4, //face top
			    2, 4, 5,
                1, 2, 5, //face right
			    1, 5, 6,
                0, 7, 4, //face left
			    0, 4, 3,
                5, 4, 7, //face back
			    5, 7, 6,
                0, 6, 7, //face bottom
			    0, 1, 6
            };
            var cube = new GameObject("TW Shock Cube Visualizer");

            GameObject.DontDestroyOnLoad(cube);
            var meshRenderer = cube.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = cube.AddComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.Optimize();
            mesh.RecalculateNormals();

            Material material = new Material(Shader.Find("Standard"));
            material.color = ColorHidden;
            //https://answers.unity.com/questions/1575599/turning-standard-materials-from-opaque-to-transpar.html
            //the question code actually worked -_-
            material.renderQueue = 3000;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // assign the material to the renderer
            meshRenderer.material = material;

            cube.transform.localScale = new Vector3(99, 0.01f, 99);
            cube.SetActive(false);

            return cube;
        }
    }

    public class PiShockJsonAuth
    {
        public string Apikey { get; set; } = null;
    }

    public class PiShockJsonLog
    {
        public string Code { get; set; }
        public int Page = 0;
    }

    public class PiShockGetToken
    {
        public string Name { get; set; } = null;
        public string Code { get; set; } = null;
    }

    public class PiShockShortCodeResp
    {
        public string Key { get; set; }
        public string ShockerName { get; set; }
    }

    public class PiShockJsonCommand : PiShockJsonAuth
    {
        public string Name { get; set; } = null;
        public ShockOperation? Op { get; set; } = null;
        public int? Duration { get; set; } = null;
        public int? Intensity { get; set; } = null;
    }

    public class PiShockerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Paused { get; set; }
        public float MaxIntensity { get; set; }
        public float MaxDuration { get; set; }
        public bool Online { get; set; }
    }

    public class PiShockerLog
    {
        public string Username { get; set; }
        public string Tm { get; set; }
        public int Code { get; set; }
        public int Duration { get; set; }
        public int Intensity { get; set; }
        public int Op { get; set; }
        public int Type { get; set; }
        public string Origin { get; set; }
    }
}
