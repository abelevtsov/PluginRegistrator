using System;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginRegistrator.DataContracts.Json.Converters
{
    internal class MoneyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.IsAssignableFrom(typeof(Money));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var jtoken = JToken.Load(reader);
                var value = jtoken.Value<decimal>();
                return new Money(value);
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
                case Money money when money.Value != default:
                    serializer.Serialize(writer, money.Value);
                    break;
                default:
                    serializer.Serialize(writer, string.Empty);
                    break;
            }
        }
    }
}
