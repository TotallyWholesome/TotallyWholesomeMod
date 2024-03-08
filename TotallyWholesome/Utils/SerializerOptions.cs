using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TotallyWholesome.Utils;

public static class SerializerOptions
{

    public static JsonSerializerSettings CamelCaseSettings = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy(),
        },
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };
}