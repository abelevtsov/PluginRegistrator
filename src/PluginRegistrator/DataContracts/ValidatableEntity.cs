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
        private readonly IDictionary<Type, Func<RangeAttribute, object, bool>> validators = new Dictionary<Type, Func<RangeAttribute, object, bool>>();

        private readonly IDictionary<string, object> tempAttributes = new Dictionary<string, object>();

        private Type currentType;

        protected ValidatableEntity(string entityLogicalName, bool immediateValidation = false)
            : base(entityLogicalName)
        {
            ModelErrors = new Dictionary<string, string>();
            ImmediateValidation = immediateValidation;
            if (!ImmediateValidation)
            {
                return;
            }

            validators[typeof(string)] =
                (a, v) =>
                {
                    var length = ((string)v).Length;
                    return (int)a.Minimum <= length && length <= (int)a.Maximum;
                };
            validators[typeof(int?)] =
                (a, v) =>
                {
                    var intValue = (int)v;
                    return (int)a.Minimum <= intValue && intValue <= (int)a.Maximum;
                };
            validators[typeof(decimal?)] =
                (a, v) =>
                {
                    var decValue = (decimal)v;
                    return decimal.Parse(a.Minimum.ToString()) <= decValue && decValue <= decimal.Parse(a.Maximum.ToString());
                };
            validators[typeof(double?)] =
                (a, v) =>
                {
                    var doubleValue = (double)v;
                    return (double)a.Minimum <= doubleValue && doubleValue <= (double)a.Maximum;
                };
        }

        public IDictionary<string, string> ModelErrors { get; private set; }

        public bool IsValid
        {
            get { return !ModelErrors.Any(); }
        }

        private Type CurrentType
        {
            get { return currentType ?? (currentType = GetType()); }
        }

        private bool ImmediateValidation { get; set; }

        public T GetTempAttribute<T>(string tempAttributeName)
        {
            object value;
            if (tempAttributes.TryGetValue(tempAttributeName, out value))
            {
                return (T)value;
            }

            return default(T);
        }

        public void SetTempAttribute(string tempAttributeName, object value)
        {
            tempAttributes[tempAttributeName] = value;
        }

        protected static T FromJson<T>(string json) where T : Entity
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        protected static T FromXml<T>(string xml) where T : Entity
        {
            return FromXml<T>(XDocument.Parse(xml));
        }

        protected static T FromXml<T>(XObject xml) where T : Entity
        {
            var json = JsonConvert.SerializeXNode(xml, Formatting.None, true);
            return FromJson<T>(json);
        }

        protected string ToJson<T>() where T : Entity
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new ProxyClassContractResolver<T>()
            };
            return JsonConvert.SerializeObject(this, settings);
        }

        protected string ToXml<T>() where T : Entity
        {
            return JsonConvert.DeserializeXNode(ToJson<T>(), LogicalName).ToString();
        }

        protected void SetAttributeValue(string propertyName, string attributeLogicalName, object value)
        {
            if (value == null || !ImmediateValidation)
            {
                SetAttributeValue(attributeLogicalName, value);
                return;
            }

            var pi = CurrentType.GetProperty(propertyName);
            var attr = pi.GetCustomAttribute<RangeAttribute>();
            if (attr == null)
            {
                SetAttributeValue(attributeLogicalName, value);
                return;
            }

            Func<RangeAttribute, object, bool> validator;
            if (validators.TryGetValue(pi.PropertyType, out validator) && validator(attr, value))
            {
                SetAttributeValue(attributeLogicalName, value);
            }
            else
            {
                ModelErrors.Add(propertyName, string.Format("{0}: введенное значение не входит в допустимый диапазон.", value));
            }
        }
    }
}
