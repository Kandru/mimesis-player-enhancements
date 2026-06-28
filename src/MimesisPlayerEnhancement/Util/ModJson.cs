using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MimesisPlayerEnhancement.Util
{
    internal static class ModJson
    {
        private static readonly JsonSerializerSettings Settings = CreateSettings();

        internal static string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        internal static T? Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings);
            }
            catch
            {
                return null;
            }
        }

        private static JsonSerializerSettings CreateSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new FieldCamelCaseContractResolver(),
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            };
        }

        private sealed class FieldCamelCaseContractResolver : DefaultContractResolver
        {
            public FieldCamelCaseContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy();
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, MemberSerialization.Fields);
            }
        }
    }
}
