using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CrmPluginAttributes;
using CrmSdk;
using Microsoft.Xrm.Sdk;
using PluginRegistrator.Entities;
using PluginRegistrator.Helpers;

namespace PluginRegistrator.Extensions
{
    public static class CrmPluginAssemblyExtensions
    {
        public static void SetupAssemblyPlugins(
            this CrmPluginAssembly pluginAssembly,
            Assembly assembly,
            IReadOnlyCollection<XElement> unsecureConfigItems,
            IEnumerable<SdkMessage> messages,
            IEnumerable<SdkMessageFilter> messageFilters)
        {
            Version sdkVersion = null;
            var types =
                    assembly
                        .GetExportedTypes()
                        .Where(
                            t =>
                                !t.IsAbstract &&
                                t.IsClass &&
                                (t.Name.EndsWith("Plugin") || t.Name.EndsWith("Activity")));
            foreach (var t in types)
            {
                CrmPluginType type;
                CrmPluginIsolatable isolatable;

                var xrmPlugin = t.GetInterface(typeof(IPlugin).FullName);
                if (xrmPlugin != null)
                {
                    type = CrmPluginType.Plugin;
                    isolatable = CrmPluginIsolatable.Yes;
                    if (sdkVersion == null)
                    {
                        sdkVersion = xrmPlugin.Assembly.GetName().Version;
                        pluginAssembly.SdkVersion = new Version(xrmPlugin.Assembly.GetName().Version.Major, xrmPlugin.Assembly.GetName().Version.Minor);
                    }

                    pluginAssembly.SdkVersion = new Version(sdkVersion.Major, sdkVersion.Minor);
                }
                else if (t.IsSubclassOf(typeof(Activity)))
                {
                    type = CrmPluginType.WorkflowActivity;
                    isolatable = CrmPluginIsolatable.No;
                }
                else
                {
                    throw new Exception("Class is not plugin or workflow");
                }

                var plugin =
                    new CrmPlugin
                        {
                            TypeName = t.FullName,
                            PluginType = type,
                            AssemblyId = pluginAssembly.AssemblyId,
                            AssemblyName = pluginAssembly.Name,
                            Isolatable = isolatable,
                            FriendlyName = Guid.NewGuid().ToString()
                        };

                if (type == CrmPluginType.WorkflowActivity)
                {
                    var attr = t.GetCustomAttribute<WorkflowActivityAttribute>();
                    plugin.WorkflowActivityGroupName = " " + attr.WorkflowActivityGroupName;
                    plugin.Name = " " + attr.Name;
                }

                var pluginEntityType = t.BaseType?.GetGenericArguments().LastOrDefault();
                if (pluginEntityType == null)
                {
                    // note: be sure - it is workflow:)
                    pluginAssembly.AddPlugin(plugin);
                    continue;
                }

                var splitted = t.FullName?.Split('.').Reverse().Take(2).ToArray();
                var typeName = splitted?[0].Replace("Plugin", string.Empty);
                plugin.Name = $" {splitted?[1]}: {typeName}";

                var stepMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.DeclaringType == t).ToArray();
                foreach (var stepMethod in stepMethods)
                {
                    var pluginStepAttrs = stepMethod.GetCustomAttributes<PluginStepAttribute>();
                    var filteringAttributes = stepMethod.GetCustomAttribute<FilteringAttributesAttribute>();
                    foreach (var pluginStepAttr in pluginStepAttrs)
                    {
                        var step =
                            new CrmPluginStep
                                {
                                    AssemblyId = plugin.AssemblyId,
                                    PluginId = plugin.Id,
                                    DeleteAsyncOperationIfSuccessful = pluginStepAttr.DeleteAsyncOperationIfSuccessful,
                                    Deployment = CrmPluginStepDeployment.ServerOnly,
                                    Enabled = pluginStepAttr.Enabled,
                                    Name = RegistrationHelper.GenerateStepName(typeName, pluginStepAttr.PluginMessageName, pluginStepAttr.EntityLogicalName.ToLowerInvariant(), null),
                                    Rank = pluginStepAttr.Rank,
                                    Stage = (CrmPluginStepStage)pluginStepAttr.Stage,
                                    Mode = pluginStepAttr.ExecutionMode == PluginExecutionMode.Asynchronous ? CrmPluginStepMode.Asynchronous : CrmPluginStepMode.Synchronous,
                                    MessageId = messages.First(m => m.Name == pluginStepAttr.PluginMessageName).Id
                                };
                        var filter =
                            messageFilters.FirstOrDefault(
                                f =>
                                    f.SdkMessageId.Id == step.MessageId &&
                                    f.PrimaryEntityLogicalName == pluginStepAttr.EntityLogicalName.ToLowerInvariant());
                        if (filter == null)
                        {
                            throw new Exception($"{pluginStepAttr.EntityLogicalName} entity doesn't registered yet");
                        }

                        step.MessageEntityId = filter.Id;
                        if (filteringAttributes != null)
                        {
                            step.FilteringAttributes = filteringAttributes.ToString();
                        }

                        var unsecureItem = unsecureConfigItems.FirstOrDefault(it => it.Attribute("key").Value == pluginStepAttr.UnsecureConfig);
                        if (unsecureItem != null)
                        {
                            var value = unsecureItem.Attribute("value").Value;
                            step.UnsecureConfiguration = value == $"#{{{pluginStepAttr.UnsecureConfig}}}" ? unsecureItem.Attribute("default").Value : value;
                        }

                        foreach (var p in stepMethod.GetParameters().Where(p => p.ParameterType == pluginEntityType))
                        {
                            var imageParameters = p.GetCustomAttribute<ImageParametersAttribute>();
                            if (imageParameters == null)
                            {
                                continue;
                            }

                            CrmPluginImage image;
                            switch (p.Name)
                            {
                                case "preEntityImage":
                                    image = CreateImage(step, imageParameters.ToString(), pluginStepAttr.PluginMessageName, CrmPluginImageType.PreImage);
                                    step.AddImage(image);
                                    break;
                                case "postEntityImage":
                                    image = CreateImage(step, imageParameters.ToString(), pluginStepAttr.PluginMessageName, CrmPluginImageType.PostImage);
                                    step.AddImage(image);
                                    break;
                            }
                        }

                        plugin.AddStep(step);
                    }
                }

                pluginAssembly.AddPlugin(plugin);
            }
        }

        public static void SetupAssemblyPlugins(
            this CrmPluginAssembly pluginAssembly,
            Version sdkVersion,
            XDocument config,
            IEnumerable<SdkMessage> messages,
            IEnumerable<SdkMessageFilter> messageFilters)
        {
            XNamespace ns = "http://schemas.microsoft.com/crm/2011/tools/pluginregistration";
            var pluginElements = config.Root.Element(ns + "Solutions").Element(ns + "Solution").Element(ns + "PluginTypes").Elements(ns + "Plugin");
            foreach (var pluginElement in pluginElements)
            {
                CrmPluginType type;
                CrmPluginIsolatable isolatable;

                var typeName = pluginElement.Attribute("TypeName").Value;
                if (typeName.EndsWith("Plugin"))
                {
                    type = CrmPluginType.Plugin;
                    isolatable = CrmPluginIsolatable.Yes;
                    if (sdkVersion != null)
                    {
                        pluginAssembly.SdkVersion = new Version(sdkVersion.Major, sdkVersion.Minor);
                    }
                }
                else if (typeName.EndsWith("Activity"))
                {
                    type = CrmPluginType.WorkflowActivity;
                    isolatable = CrmPluginIsolatable.No;
                }
                else
                {
                    throw new Exception("Class is not plugin or workflow");
                }

                var plugin =
                    new CrmPlugin
                        {
                            TypeName = typeName,
                            PluginType = type,
                            AssemblyId = pluginAssembly.AssemblyId,
                            AssemblyName = pluginAssembly.Name,
                            Isolatable = isolatable,
                            FriendlyName = pluginElement.Attribute("FriendlyName").Value
                        };

                if (type == CrmPluginType.WorkflowActivity)
                {
                    plugin.WorkflowActivityGroupName = " " + pluginElement.Attribute("FriendlyName").Value;
                    plugin.Name = " " + pluginElement.Attribute("Name").Value;
                    pluginAssembly.AddPlugin(plugin);
                    continue;
                }

                var splitted = typeName.Split('.').Reverse().Take(2).ToArray();
                var pluginName = splitted[0].Replace("Plugin", string.Empty);
                plugin.Name = $" {splitted[1]}: {pluginName}";

                foreach (var pluginStepEl in pluginElement.Element(ns + "Steps").Elements(ns + "Step"))
                {
                    var step =
                        new CrmPluginStep
                            {
                                AssemblyId = plugin.AssemblyId,
                                PluginId = plugin.Id,
                                DeleteAsyncOperationIfSuccessful = false, // ToDo: implement for ugly config
                                Deployment = CrmPluginStepDeployment.ServerOnly,
                                Enabled = true, // ToDo: implement for ugly config
                                Name = pluginStepEl.Attribute("Name").Value,
                                Rank = int.Parse(pluginStepEl.Attribute("Rank").Value),
                                Stage = pluginStepEl.Attribute("Stage").Value == "PreInsideTransaction" ? CrmPluginStepStage.PreOperation : CrmPluginStepStage.PostOperation, // ToDo: CrmPluginStepStage.PreValidation
                                Mode = pluginStepEl.Attribute("Mode").Value == PluginExecutionMode.Asynchronous.ToString().ToLowerInvariant() ? CrmPluginStepMode.Asynchronous : CrmPluginStepMode.Synchronous,
                                MessageId = messages.First(m => m.Name == pluginStepEl.Attribute("MessageName").Value).Id,
                                Description = pluginStepEl.Attribute("Description").Value
                            };
                    var filter =
                        messageFilters.FirstOrDefault(
                            f =>
                                f.SdkMessageId.Id == step.MessageId &&
                                f.PrimaryEntityLogicalName == pluginStepEl.Attribute("PrimaryEntityName").Value.ToLowerInvariant());
                    if (filter == null)
                    {
                        throw new Exception($"{pluginStepEl.Attribute("PrimaryEntityName").Value} entity doesn't registered yet");
                    }

                    step.MessageEntityId = filter.Id;
                    var attr = pluginStepEl.Attribute("FilteringAttributes");
                    if (!string.IsNullOrEmpty(attr?.Value))
                    {
                        step.FilteringAttributes = attr.Value;
                    }

                    var unsecureItem = pluginStepEl.Attribute("CustomConfiguration").Value;
                    if (!string.IsNullOrEmpty(unsecureItem))
                    {
                        step.UnsecureConfiguration = unsecureItem;
                    }

                    foreach (var imageEl in pluginStepEl.Element(ns + "Images").Elements(ns + "Image"))
                    {
                        var image = CreateImage(step, imageEl, pluginStepEl);
                        if (image != null)
                        {
                            step.AddImage(image);
                        }
                    }

                    plugin.AddStep(step);
                }

                pluginAssembly.AddPlugin(plugin);
            }
        }

        private static CrmPluginImage CreateImage(CrmPluginStep step, XElement imageEl, XElement pluginStepEl)
        {
            var imageAttributes = imageEl.Attribute("Attributes");
            var imageType = imageEl.Attribute("ImageType").Value;

            // note: ignore EntityAlias for convenience
            switch (imageType)
            {
                case "PreImage":
                    return CreateImage(
                        step,
                        string.IsNullOrEmpty(imageAttributes?.Value) ? null : imageAttributes.Value,
                        pluginStepEl.Attribute("MessageName").Value,
                        CrmPluginImageType.PreImage);
                case "PostImage":
                    return CreateImage(
                        step,
                        string.IsNullOrEmpty(imageAttributes?.Value) ? null : imageAttributes.Value,
                        pluginStepEl.Attribute("MessageName").Value,
                        CrmPluginImageType.PostImage);
                case "Both":
                    // ToDo: process "Both"
                    return null;
                default:
                    return null;
            }
        }

        private static CrmPluginImage CreateImage(CrmPluginStep step, string imageAttributes, string pluginMessageName, CrmPluginImageType pluginImageType)
        {
            return
                new CrmPluginImage(
                    step.AssemblyId,
                    step.PluginId,
                    step.Id,
                    imageAttributes,
                    null,
                    pluginImageType == CrmPluginImageType.PreImage ? "preimage" : "postimage",
                    pluginImageType,
                    CrmMessage.Instance[pluginMessageName],
                    pluginImageType == CrmPluginImageType.PreImage ? "preimage" : "postimage");
        }
    }
}
