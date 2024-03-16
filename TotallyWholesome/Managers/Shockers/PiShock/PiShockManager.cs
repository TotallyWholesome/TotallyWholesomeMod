#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using Newtonsoft.Json;
using OneOf;
using OneOf.Types;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers.PiShock.Config;
using TotallyWholesome.Managers.TWUI.Pages.Shocker;
using TotallyWholesome.Utils;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.ControlPackets.Shockers.Models;
using WholesomeLoader;

namespace TotallyWholesome.Managers.Shockers.PiShock;

public class PiShockManager : IShockerProvider, IAsyncDisposable
{
    public static PiShockManager Instance { get; private set; } = null!;

    private const string PiShockName = "TotallyWholesome";
    private const string PiShockBaseUrl = "https://do.pishock.com/api/";

    // Got that poggers route namings, gg...
    private const string PiShockApiOperate = "shortapiOperate";
    private const string PiShockApiInfo = "getshockerinfoshort";
    private const string PiShockGetKey = "GetKeyAndNameFromShort";
    private const string PiShockGetLogs = "getlastlogsfromshort";

    private static readonly MelonLogger.Instance Logger = new("TotallyWholesome.PiShock.Config", Color.Green);

    private readonly HttpClient _httpClient = new HttpClient();

    public PiShockManager()
    {
        Instance = this;
        _httpClient.BaseAddress = new Uri(PiShockBaseUrl);

        TwTask.Run(UpdateShockers);
    }
    
    public async Task<OneOf<Success, AddFailedHttpError, Error>> AddShareCode(string code)
    {
        var keyRequest = new PiShockGetToken
        {
            Name = PiShockName,
            Code = code
        };
        var keyRequestJson = JsonConvert.SerializeObject(keyRequest);
        var response = await _httpClient.PostAsync(PiShockGetKey,
            new StringContent(keyRequestJson, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
            return new AddFailedHttpError
            {
                StatusCode = response.StatusCode,
                Body = await response.Content.ReadAsStringAsync()
            };

        var key = JsonConvert.DeserializeObject<PiShockShortCodeResp>(await response.Content.ReadAsStringAsync());

        if (key == null)
        {
            Con.Error("Failed to register shocker! Could not deserialize response!");
            return new Error();
        }

        Con.Debug($"Got PiShockShortCodeResp: {key.Key}|{key.ShockerName}");

        PiShockConfig.Config.Shockers.Add(key.Key, new PiShockConfig.PiShockConf.ShockerConfig
        {
            Enabled = true
        });
        PiShockConfig.SaveFnF();

#pragma warning disable CS4014
        TwTask.Run(UpdateShockers());
#pragma warning restore CS4014
        
        //Successful
        return new Success();
    }

    private async Task<PiShockerInfo> GetShockerInfo(string key)
    {
        try
        {
            var auth = new PiShockJsonAuth()
            {
                Apikey = key,
            };

            var authJson = JsonConvert.SerializeObject(auth, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var response = await _httpClient.PostAsync(PiShockApiInfo,
                new StringContent(authJson, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var info = JsonConvert.DeserializeObject<PiShockerInfo>(await response.Content.ReadAsStringAsync());

            return info;
        }
        catch (Exception e)
        {
            Con.Warn("A problem occured while trying to retrieve shocker info!");
            Con.Warn(e);
        }

        return null;
    }

    private async Task<PiShockerLog[]> GetShockerLog(string code)
    {
        try
        {
            var auth = new PiShockJsonLog()
            {
                Code = code,
            };

            var authJson = JsonConvert.SerializeObject(auth, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var response = await _httpClient.PostAsync(PiShockGetLogs,
                new StringContent(authJson, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var a = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<PiShockerLog[]>(a);
        }
        catch (Exception e)
        {
            Con.Error("A problem occured while trying to retrieve shocker logs!");
            Con.Error(e);
        }

        return null;
    }

    private readonly IDictionary<string, PiShockerInfo> _shockerInfoCache = new Dictionary<string, PiShockerInfo>();

    /// <summary>
    /// Update all shockers we have codes for
    /// </summary>
    public async Task UpdateShockers()
    {
        try
        {
            foreach (var (key, value) in PiShockConfig.Config.Shockers)
            {
                _shockerInfoCache[key] = await GetShockerInfo(key);
            }
        }
        catch (Exception e)
        {
            Logger.Error("Error occurred while updating shockers", e);
        }

        
        Main.Instance.MainThreadQueue.Enqueue(() => PiShockPage.UpdateShockerInfo(_shockerInfoCache));
    }

    public async Task Control(ControlType type, byte intensity, ushort duration)
    {
        var piShockOperation = PiShockUtils.OperationTranslation[type];

        var selectedShockers = PiShockConfig.Config.Shockers.Where(x => x.Value.Enabled).ToArray();
        if (ConfigManager.Instance.IsActiveCurrent(AccessType.ShockRandomShocker) && selectedShockers.Length > 0)
            selectedShockers = [selectedShockers[new Random().Next(0, selectedShockers.Length)]];

        foreach (var (key, value) in selectedShockers)
        {
            if (!_shockerInfoCache.TryGetValue(key, out var shockerInfo))
            {
                Logger.Warning($"No shocker info found for key: {key}");
                return;
            }

            var command = new PiShockJsonCommand
            {
                Apikey = key,
                Name = LeadManager.Instance?.MasterPair?.Master?.Username ?? PiShockName,
                Op = piShockOperation,
                Duration = Convert.ToInt32(Math.Clamp(duration / 1000f, 0, shockerInfo.MaxDuration)),
                Intensity = Convert.ToInt32(intensity / 100f * shockerInfo.MaxIntensity)
            };

            var commandsJson = JsonConvert.SerializeObject(command, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            await _httpClient.PostAsync(PiShockApiOperate,
                new StringContent(commandsJson, Encoding.UTF8, "application/json"));
        }
    }

    public bool NoLimits { get; }

    private bool _disposed;
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        Instance = null!;
        _httpClient.Dispose();
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

public struct AddFailedHttpError
{
    public HttpStatusCode StatusCode { get; set; }
    public string Body { get; set; }
}