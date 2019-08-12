using System;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginRegistrator.DataContracts.Json.Converters
{
    internal class EntityReferenceConverter : JsonConverter
    {
        public EntityReferenceConverter(string entityLogicalName) => EntityLogicalName = entityLogicalName;

        private string EntityLogicalName { get; set; }

        public override bool CanConvert(Type objectType) => objectType.IsAssignableFrom(typeof(EntityReference));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var jtoken = JToken.Load(reader);
                var name = jtoken.Value<string>();
                return Guid.TryParse(name, out var id)
                    ? new EntityReference(EntityLogicalName, id)
                    : new EntityReference(EntityLogicalName, Guid.Empty) { Name = name };
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
                case EntityReference entityReference when entityReference.Id != Guid.Empty:
                    serializer.Serialize(writer, entityReference.Id.ToString());
                    break;
                case EntityReference _:
                    serializer.Serialize(writer, string.Empty);
                    break;
                default:
                    serializer.Serialize(writer, string.Empty);
                    break;
            }
        }
    }
}
