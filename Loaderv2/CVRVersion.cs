using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace WholesomeLoader
{
    public class CVRVersion
    {
        public string VersionString;
        public readonly int Year = 0;
        public readonly int Release = 0;
        public readonly int? Patch;
        public readonly int? Experimental;
        public bool AllVersions;

        private static Regex _versionMatch = new Regex("^(?'year'\\d{4})r(?'release'\\d*)(?>ex(?'experimental'\\d*)|p(?'patch'\\d*))*", RegexOptions.Compiled);

        private bool invalidVersion;

        public CVRVersion(string version)
        {
            VersionString = version;
            
            if (version == "*" || version == "all" || version == null)
            {
                AllVersions = true;
                invalidVersion = true;
                return;
            }
            
            version = version.ToLower();

            var match = _versionMatch.Match(version);

            if (!match.Success)
            {
                invalidVersion = true;
                return;
            }

            Year = Convert.ToInt32(match.Groups["year"].Value);
            Release = Convert.ToInt32(match.Groups["release"].Value);

            var exp = match.Groups.FirstOrDefault(x => x.Name == "experimental");

            if (exp != null && !string.IsNullOrWhiteSpace(exp.Value)) 
                Experimental = Convert.ToInt32(exp.Value);

            var patch = match.Groups.FirstOrDefault(x => x.Name == "patch");

            if (patch != null && !string.IsNullOrWhiteSpace(patch.Value)) 
                Patch = Convert.ToInt32(patch.Value);
        }

        public int IsVersionNewer(CVRVersion target)
        {
            if (AllVersions || target.AllVersions) return 0;

            if (invalidVersion && !target.invalidVersion) return -1;
            if (invalidVersion && target.invalidVersion) return 0;
            if (!invalidVersion && target.invalidVersion) return 1;
            
            if (Equals(target)) return 0;

            if (Year < target.Year) return -1;
            if (Release < target.Release) return -1;
            if (Experimental.HasValue && !target.Experimental.HasValue) return -1;
            if (Experimental.HasValue && target.Experimental.HasValue && Experimental.Value < target.Experimental.Value) return -1;
            if (!Patch.HasValue && target.Patch.HasValue) return -1;
            if (Patch.HasValue && target.Patch.HasValue && Patch.Value < target.Patch.Value) return -1;

            return 1;
        }

        public override bool Equals(object obj)
        {
            CVRVersion target = obj as CVRVersion;
            if (target == null) return false;

            //Invalid versions will not ever be equal, all versions should have valid data
            if (invalidVersion || target.invalidVersion) return false;

            if (Year != target.Year) return false;
            if (Release != target.Release) return false;
            if (Patch.HasValue != target.Patch.HasValue) return false;
            if (Patch.HasValue && target.Patch.HasValue && Patch.Value != target.Patch.Value) return false;
            if (Experimental.HasValue != target.Experimental.HasValue) return false;
            if (Experimental.HasValue && target.Experimental.HasValue && Experimental.Value != target.Experimental.Value) return false;

            return true;
        }

        protected bool Equals(CVRVersion other)
        {
            return Year == other.Year && Release == other.Release && Patch == other.Patch && Experimental == other.Experimental;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Year, Release, Patch, Experimental);
        }
    }
}