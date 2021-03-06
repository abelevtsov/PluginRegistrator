﻿using System;
using System.Collections.Generic;

namespace PluginRegistrator
{
    public class CrmMessage
    {
        private static readonly Lazy<CrmMessage> InstanceHolder = new Lazy<CrmMessage>(() => new CrmMessage());

        private static readonly IDictionary<string, string> MessagePropertyNames =
            new Dictionary<string, string>
                {
                    { "Create", "Id" },
                    { "Assign", "Target" },
                    { "Update", "Target" },
                    { "Delete", "Target" },
                    { "Merge", "Target,SubordinateId" },
                    { "SetState", "EntityMoniker" },
                    { "SetStateDynamicEntity", "EntityMoniker" }
                };

        private CrmMessage()
        {
        }

        public static CrmMessage Instance => InstanceHolder.Value;

        public string this[string methodName] =>
            MessagePropertyNames.TryGetValue(methodName.Replace("On", string.Empty), out var value)
                ? value
                : null;
    }
}
