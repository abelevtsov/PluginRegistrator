using System;
using System.Reflection;

using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PluginRegistrator.DataContracts.Json
{
    public class ProxyClassContractResolver<TEntity> : DefaultContractResolver where TEntity : Entity
    {
        private readonly Type currentType = typeof(TEntity);

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.DeclaringType != currentType)
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
