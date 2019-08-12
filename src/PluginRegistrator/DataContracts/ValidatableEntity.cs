using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using PluginRegistrator.DataContracts.Json;

namespace PluginRegistrator.DataContracts
{
    [DataContract]
    public abstract class ValidatableEntity : Entity
    {
        private readonly IDictionary<string, object> tempAttributes = new Dictionary<string, object>();

        private Type currentType;

        protected ValidatableEntity(string entityLogicalName, bool immediateValidation = false)
            : base(entityLogicalName) =>
                ImmediateValidation = immediateValidation;

        public IDictionary<string, string> ModelErrors { get; } = new Dictionary<string, string>();

        public bool IsValid => !ModelErrors.Any();

        private Type CurrentType => currentType ?? (currentType = GetType());

        private bool ImmediateValidation { get; set; }

        public T GetTempAttribute<T>(string tempAttributeName) =>
            tempAttributes.TryGetValue(tempAttributeName, out var value) ? (T) value : default;

        public void SetTempAttribute(string tempAttributeName, object value) => tempAttributes[tempAttributeName] = value;

        protected static T FromJson<T>(string json) where T : Entity => JsonConvert.DeserializeObject<T>(json);

        protected static T FromXml<T>(string xml) where T : Entity => FromXml<T>(XDocument.Parse(xml));

        protected static T FromXml<T>(XObject xml) where T : Entity
        {
            var json = JsonConvert.SerializeXNode(xml, Formatting.None, true);
            return FromJson<T>(json);
        }

        protected string ToJson<T>() where T : Entity
        {
            var settings =
                new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new ProxyClassContractResolver<T>()
                    };
            return JsonConvert.SerializeObject(this, settings);
        }

        protected string ToXml<T>() where T : Entity => JsonConvert.DeserializeXNode(ToJson<T>(), LogicalName).ToString();

        protected void SetAttributeValue(string propertyName, string attributeLogicalName, object value)
        {
            if (value == null || !ImmediateValidation)
            {
                SetAttributeValue(attributeLogicalName, value);
                return;
            }

            var pi = CurrentType.GetProperty(propertyName);
            var attr = pi?.GetCustomAttribute<RangeAttribute>();
            if (attr?.IsValid(value) == true)
            {
                SetAttributeValue(attributeLogicalName, value);
                return;
            }

            ModelErrors.Add(propertyName, $"{value}: value outside allowed range.");
        }
    }
}
