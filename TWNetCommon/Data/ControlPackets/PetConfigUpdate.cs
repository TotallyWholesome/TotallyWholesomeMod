using System;
using System.Collections.Generic;
using MessagePack;
using TWNetCommon.Data.NestedObjects;

namespace TWNetCommon.Data.ControlPackets;

[MessagePackObject]
public class PetConfigUpdate
{
    /// <summary>
    /// LeadPair key shared during initial request, if none given packet affects all pets
    /// </summary>
    [Key(0)]
    public string Key { get; set; }

    /// <summary>
    /// Update type for this packet
    /// </summary>
    [Key(1)]
    public UpdateType UpdateType { get; set; }

    /// <summary>
    /// List of parameters that the master has access to
    /// </summary>
    [Key(2)]
    public List<MasterRemoteParameter> Parameters { get; set; }

    /// <summary>
    /// List of avatars that the master is allowed to switch the pet into
    /// </summary>
    [Key(3)]
    public List<string> AllowedAvatars { get; set; }

    /// <summary>
    /// Contains all features/consent options the pet has enabled for this master
    /// Can be used to enable and disable parts of the UI and functions
    /// </summary>
    [Key(4)]
    public NetworkedFeature AllowedFeatures { get; set; }

    public override string ToString()
    {
        return $"PetConfigUpdate: [Key: {Key}, UpdateType: {UpdateType}, AppliedFeatures: {AllowedFeatures}]";
    }
}

[Flags]
public enum UpdateType
{
    RemoteParamUpdate = 0,
    AvatarListUpdate = 1,
    AllowedFeaturesUpdate = 2
}