using Semver;

namespace WholesomeLoader;

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
    public SemVersion MinVer;
    public SemVersion MaxVer;
    
    private bool _invalidVersion;
    private bool _wildcardMax;

    public string ExpectedCVRVersion
    {
        get => MinVer.ToString();
        set
        {
            if (!SemVersion.TryParse(value, out MinVer))
                _invalidVersion = true;
        }
    }

    public string MaxCVRVersion
    {
        get => _wildcardMax ? "*" : MaxVer.ToString();
        set
        {
            if (value == "*")
            {
                _wildcardMax = true;
                return;
            }

            if (!SemVersion.TryParse(value, out MaxVer))
                _invalidVersion = true;
        }
    }
        
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(AssemblyHash) && !string.IsNullOrWhiteSpace(AssemblyName) && !_invalidVersion;
    }

    public bool IsMinCompatible(SemVersion version)
    {
        if (_invalidVersion)
            return false;
        return SemVersion.Compare(version, MinVer) <= 0;
    }
    
    public bool IsMaxCompatible(SemVersion version)
    {
        if (_invalidVersion)
            return false;
        return SemVersion.Compare(version, MaxVer) >= 0;
    }

    public override string ToString()
    {
        return $"[TWAssemblyVersion] {Branch}, {BranchPrettyName}, {TWVersionInfo}, {AssemblyName} {AssemblyHash}";
    }
}