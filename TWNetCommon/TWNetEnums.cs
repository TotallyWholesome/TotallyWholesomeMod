using System;

namespace TWNetCommon;

[Flags]
public enum NetworkedFeature
{
    None = 0,
    AllowForceMute = 1,
    AllowToyControl = 2,
    AllowPinning = 4,
    DisableFlight = 8, //Used to indicate movement controls being enabled
    AllowBlindfolding = 16,
    AllowDeafening = 32,
    AllowBeep = 64,
    AllowVibrate = 128,
    AllowShock = 256,
    AllowHeight = 512,
    DisableSeats = 1024,
    AllowAnyAvatarSwitching = 2048,
    MasterBypassDeafen = 4096,
    ArmbindMode = 8192
}