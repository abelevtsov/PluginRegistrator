using System;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginRegistrator.DataContracts.Json.Converters
{
    internal class OptionSetValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsAssignableFrom(typeof(OptionSetValue));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var jtoken = JToken.Load(reader);
                return new OptionSetValue(jtoken.Value<int>());
            }
            catch
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case OptionSetValue option when option.Value != default:
                    serializer.Serialize(writer, option.Value);
                    break;
                default:
                    serializer.Serialize(writer, string.Empty);
                    break;
            }
        }
    }
}
