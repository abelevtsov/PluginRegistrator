using System;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginRegistrator.DataContracts.Json.Converters
{
    internal class MoneyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Money));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jtoken = JToken.Load(reader);
            try
            {
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
            var money = value as Money;
            if (money == null || money.Value == 0M)
            {
                serializer.Serialize(writer, string.Empty);
            }
            else
            {
                serializer.Serialize(writer, money.Value);
            }
        }
    }
}
