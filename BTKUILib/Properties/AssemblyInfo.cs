using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("BTKUILib")]
[assembly: AssemblyDescription("BTK CVR Core UI library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("BTKDevelopment")]
[assembly: AssemblyProduct("BTKUILib")]
[assembly: AssemblyCopyright("BTKDevelopment Copyright © 2022")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4BA58EB4-181A-4661-86F3-6E45DCB88DAE")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.0.2.0")]
[assembly: AssemblyFileVersion("2.0.2.0")]
[assembly: MelonInfo(typeof(BTKUILib.BTKUILib), BTKUILib.BuildInfo.Name, BTKUILib.BuildInfo.Version, BTKUILib.BuildInfo.Author)]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonPriority(-10)]
[assembly: HarmonyDontPatchAll]