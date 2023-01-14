using System;
using MessagePack;

namespace TWNetCommon.Data.NestedObjects
{
    [MessagePackObject]
    public class MasterRemoteParameter
    {
        /// <summary>
        /// Parameter target will be used clientside to determine what we are affecting
        /// </summary>
        [Key(0)]
        public string ParameterTarget { get; set; }
        /// <summary>
        /// Taking a bit of a thing out of ParamLibs playbook, all parameters will be made into floats to simplify the data
        /// tho I could use serialization instead I guess :AkkoShrug:
        /// </summary>
        [Key(1)]
        public float ParameterValue { get; set; }
        /// <summary>
        /// Contains the VRChat AV3 parameter type
        /// </summary>
        [Key(2)]
        public int ParameterType { get; set; }
        
        /// <summary>
        /// Local use only, this parameter will only be used to determine if a parameter needs to be sent
        /// </summary>
        [Key(3)]
        public bool IsUpdated { get; set; }
        
        [Key(4)]
        public string[] ParameterOptions { get; set; }
    }
}