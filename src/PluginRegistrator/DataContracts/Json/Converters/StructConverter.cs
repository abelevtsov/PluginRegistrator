using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginRegistrator.DataContracts.Json.Converters
{
    internal class StructConverter<T> : JsonConverter where T : struct
    {
        public override bool CanConvert(Type objectType) => objectType.IsAssignableFrom(typeof(T));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var jtoken = JToken.Load(reader);
                var type = typeof(T);

                if (type == typeof(Guid))
                {
                    var value = jtoken.Value<string>();
                    return string.IsNullOrWhiteSpace(value) ? (Guid?)null : Guid.Parse(value);
                }

                if (type == typeof(bool))
                {
                    var value = jtoken.Value<string>().ToLowerInvariant();
                    return string.IsNullOrWhiteSpace(value)
                        ? (bool?)null
                        : value == "1" || value == bool.TrueString.ToLowerInvariant();
                }

                return jtoken.Value<T>();
            }
            catch
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value ?? string.Empty);
        }
    }
}
