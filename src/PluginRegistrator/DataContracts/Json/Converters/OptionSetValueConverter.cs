using System;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginRegistrator.DataContracts.Json.Converters
{
    internal class OptionSetValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(OptionSetValue));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jtoken = JToken.Load(reader);
            try
            {
                return new OptionSetValue(jtoken.Value<int>());
            }
            catch
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var option = value as OptionSetValue;
            if (option == null || option.Value == 0)
            {
                serializer.Serialize(writer, string.Empty);
            }
            else
            {
                serializer.Serialize(writer, option.Value);
            }
        }
    }
}
