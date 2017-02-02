using System;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginRegistrator.DataContracts.Json.Converters
{
    internal class EntityReferenceConverter : JsonConverter
    {
        public EntityReferenceConverter(string entityLogicalName)
        {
            EntityLogicalName = entityLogicalName;
        }

        private string EntityLogicalName { get; set; }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(EntityReference));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jtoken = JToken.Load(reader);
            try
            {
                var name = jtoken.Value<string>();
                Guid id;
                if (Guid.TryParse(name, out id))
                {
                    return new EntityReference(EntityLogicalName, id);
                }

                return new EntityReference(EntityLogicalName, Guid.Empty)
                           {
                               Name = name
                           };
            }
            catch
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var reference = value as EntityReference;
            if (reference == null)
            {
                serializer.Serialize(writer, string.Empty);
            }
            else
            {
                var id = ((EntityReference)value).Id;
                serializer.Serialize(writer, id == Guid.Empty ? string.Empty : id.ToString());
            }
        }
    }
}
