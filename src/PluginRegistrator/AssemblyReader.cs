using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CrmPluginAttributes;
using CrmSdk;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PluginRegistrator.Entities;

namespace PluginRegistrator
{
    public static class AssemblyReader
    {
        private static IEnumerable<SdkMessage> messages;

        private static IEnumerable<SdkMessageFilter> messageFilters;

        internal static IOrganizationService OrgService { private get; set; }

        private static IEnumerable<SdkMessage> Messages => messages ?? (messages = RegistrationHelper.RetrieveMessages());

        private static IEnumerable<SdkMessageFilter> MessageFilters => messageFilters ?? (messageFilters = RegistrationHelper.RetrieveMessageFilters(Messages.Select(m => m.Id).ToArray()));

        public static CrmPluginAssembly RetrievePluginsFromAssembly(string path, string pathToUnsecureConfigFile)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException("Path does not point to an existing file");
            }

            if (string.IsNullOrEmpty(pathToUnsecureConfigFile))
            {
                throw new ArgumentNullException(nameof(pathToUnsecureConfigFile));
            }

            Contract.EndContractBlock();

            var unsecureConfigItems = XDocument.Load(pathToUnsecureConfigFile).Root.Elements("item").ToList();
            var assembly = LoadAssembly(path);
            var pluginAssembly = assembly.ToCrmPluginAssembly();
            pluginAssembly.FillPluginsFromAssembly(assembly, unsecureConfigItems);

            return pluginAssembly;
        }

        public static CrmPluginAssembly RetrievePluginsFromAssembly(string path, XDocument config)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Contract.EndContractBlock();

            var assembly = LoadAssembly(path);
            var pluginAssembly = assembly.ToCrmPluginAssembly();
            var pluginType = assembly.GetExportedTypes().FirstOrDefault(t => !t.IsAbstract && t.IsClass && t.Name.EndsWith("Plugin"));
            Version sdkVersion = null;
            if (pluginType != null)
            {
                var xrmPlugin = pluginType.GetInterface(typeof(IPlugin).FullName);
                sdkVersion = xrmPlugin.Assembly.GetName().Version;
            }

            pluginAssembly.FillPluginsFromAssembly(sdkVersion, config);

            return pluginAssembly;
        }

        public static CrmPluginAssembly LoadAssemblyFromDB(string assemblyName)
        {
            var query = new QueryExpression(PluginAssembly.EntityLogicalName)
                            {
                                ColumnSet = new ColumnSet("path", "version", "publickeytoken", "culture", "sourcetype", "isolationmode", "description"),
                                NoLock = true
                            };
            query.Criteria.AddCondition("name", ConditionOperator.Equal, assemblyName);
            var pluginAssembly = OrgService.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (pluginAssembly != null)
            {
                pluginAssembly["name"] = assemblyName;
                var assembly = new CrmPluginAssembly(pluginAssembly.ToEntity<PluginAssembly>());
                query = new QueryExpression(PluginType.EntityLogicalName)
                            {
                                ColumnSet = new ColumnSet(true)
                            };
                query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assembly.Id);
                var plugins = OrgService.RetrieveMultiple(query).Entities.Cast<PluginType>().ToList();
                foreach (var plugin in plugins)
                {
                    var crmPlugin = new CrmPlugin(plugin);
                    query = new QueryExpression(SdkMessageProcessingStep.EntityLogicalName)
                                {
                                    ColumnSet = new ColumnSet(true)
                                };
                    query.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, plugin.Id);
                    var steps = OrgService.RetrieveMultiple(query).Entities.Cast<SdkMessageProcessingStep>().ToList();
                    foreach (var step in steps)
                    {
                        var crmStep = new CrmPluginStep(assembly.Id, step);
                        query = new QueryExpression(SdkMessageProcessingStepImage.EntityLogicalName)
                                    {
                                        ColumnSet = new ColumnSet(true)
                                    };
                        query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, step.Id);
                        var images = OrgService.RetrieveMultiple(query).Entities.Cast<SdkMessageProcessingStepImage>().ToList();
                        foreach (var image in images)
                        {
                            var crmImage = new CrmPluginImage(assembly.Id, plugin.Id, image);

                            crmStep.AddImage(crmImage);
                        }

                        crmPlugin.AddStep(crmStep);
                    }

                    assembly.AddPlugin(crmPlugin);
                }

                return assembly;
            }

            return null;
        }

        public static void UpdateAssemblyInDB(string pathToAssembly, ICrmEntity assembly, params PluginType[] workflows)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (string.IsNullOrEmpty(pathToAssembly))
            {
                throw new ArgumentNullException(nameof(pathToAssembly));
            }

            Contract.EndContractBlock();

            var pluginAssembly = assembly.ToEntity<PluginAssembly>();

            pluginAssembly.Content = Convert.ToBase64String(File.ReadAllBytes(pathToAssembly));

            if (workflows != null && workflows.Length > 0)
            {
                pluginAssembly.pluginassembly_plugintype = workflows;
            }

            OrgService.Update(pluginAssembly);
        }

        public static Guid CreateAssemblyInDB(string pathToAssembly, ICrmEntity assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (string.IsNullOrEmpty(pathToAssembly))
            {
                throw new ArgumentNullException(nameof(pathToAssembly));
            }

            Contract.EndContractBlock();

            var pluginAssembly = assembly.ToEntity<PluginAssembly>();
            pluginAssembly.Content = Convert.ToBase64String(File.ReadAllBytes(pathToAssembly));

            return OrgService.Create(pluginAssembly);
        }

        private static Assembly LoadAssembly(string path)
        {
            return Assembly.LoadFrom(path);
        }

        private static CrmPluginAssembly ToCrmPluginAssembly(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Contract.EndContractBlock();

            var pluginAssembly =
                new CrmPluginAssembly
                    {
                        SourceType = CrmAssemblySourceType.Database
                    };

            var name = assembly.GetName();
            var cultureLabel = name.CultureInfo.LCID == CultureInfo.InvariantCulture.LCID ? "neutral" : name.CultureInfo.Name;

            pluginAssembly.Name = name.Name;
            pluginAssembly.Version = name.Version.ToString();
            pluginAssembly.Culture = cultureLabel;

            var tokenBytes = name.GetPublicKeyToken();
            pluginAssembly.PublicKeyToken = tokenBytes == null || tokenBytes.Length == 0
                ? null
                : string.Join(string.Empty, tokenBytes.Select(b => b.ToString("X2")));

            return pluginAssembly;
        }

        private static void FillPluginsFromAssembly(this CrmPluginAssembly pluginAssembly, Assembly assembly, IReadOnlyCollection<XElement> unsecureConfigItems)
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
                    // be sure - it is workflow:)
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
                        // ToDo: use AutoMapper
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
                                    MessageId = Messages.First(m => m.Name == pluginStepAttr.PluginMessageName).Id
                                };
                        var filter =
                            MessageFilters.FirstOrDefault(
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

                            CrmPluginImage image = null;
                            if (p.Name == "preEntityImage")
                            {
                                image = new CrmPluginImage(step.AssemblyId, step.PluginId, step.Id, imageParameters.ToString(), null, "preimage", CrmPluginImageType.PreImage, CrmMessage.Instance[pluginStepAttr.PluginMessageName]);
                                image.Name = "preimage";
                            }

                            if (p.Name == "postEntityImage")
                            {
                                image = new CrmPluginImage(step.AssemblyId, step.PluginId, step.Id, imageParameters.ToString(), null, "postimage", CrmPluginImageType.PostImage, CrmMessage.Instance[pluginStepAttr.PluginMessageName]);
                                image.Name = "postimage";
                            }

                            if (image != null)
                            {
                                step.AddImage(image);
                            }
                        }

                        plugin.AddStep(step);
                    }
                }

                pluginAssembly.AddPlugin(plugin);
            }
        }

        private static void FillPluginsFromAssembly(this CrmPluginAssembly pluginAssembly, Version sdkVersion, XDocument config)
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

                foreach (var pluginStepAttr in pluginElement.Element(ns + "Steps").Elements(ns + "Step"))
                {
                    // ToDo: use AutoMapper
                    var step =
                        new CrmPluginStep
                            {
                                AssemblyId = plugin.AssemblyId,
                                PluginId = plugin.Id,
                                DeleteAsyncOperationIfSuccessful = false, // ToDo: implement for ugly config
                                Deployment = CrmPluginStepDeployment.ServerOnly,
                                Enabled = true, // ToDo: implement for ugly config
                                Name = pluginStepAttr.Attribute("Name").Value,
                                Rank = int.Parse(pluginStepAttr.Attribute("Rank").Value),
                                Stage = pluginStepAttr.Attribute("Stage").Value == "PreInsideTransaction" ? CrmPluginStepStage.PreOperation : CrmPluginStepStage.PostOperation, // ToDo: CrmPluginStepStage.PreValidation
                                Mode = pluginStepAttr.Attribute("Mode").Value == PluginExecutionMode.Asynchronous.ToString().ToLowerInvariant() ? CrmPluginStepMode.Asynchronous : CrmPluginStepMode.Synchronous,
                                MessageId = Messages.First(m => m.Name == pluginStepAttr.Attribute("MessageName").Value).Id,
                                Description = pluginStepAttr.Attribute("Description").Value
                            };
                    var filter =
                        MessageFilters.FirstOrDefault(
                            f =>
                                f.SdkMessageId.Id == step.MessageId &&
                                f.PrimaryEntityLogicalName == pluginStepAttr.Attribute("PrimaryEntityName").Value.ToLowerInvariant());
                    if (filter == null)
                    {
                        throw new Exception($"{pluginStepAttr.Attribute("PrimaryEntityName").Value} entity doesn't registered yet");
                    }

                    step.MessageEntityId = filter.Id;
                    var attr = pluginStepAttr.Attribute("FilteringAttributes");
                    if (!string.IsNullOrEmpty(attr?.Value))
                    {
                        step.FilteringAttributes = attr.Value;
                    }

                    var unsecureItem = pluginStepAttr.Attribute("CustomConfiguration").Value;
                    if (!string.IsNullOrEmpty(unsecureItem))
                    {
                        step.UnsecureConfiguration = unsecureItem;
                    }

                    foreach (var p in pluginStepAttr.Element(ns + "Images").Elements(ns + "Image"))
                    {
                        CrmPluginImage image = null;
                        var imageAttr = p.Attribute("Attributes");

                        // note: ignore EntityAlias for convenience
                        if (p.Attribute("ImageType").Value == "PreImage")
                        {
                            image =
                                new CrmPluginImage(
                                    step.AssemblyId,
                                    step.PluginId,
                                    step.Id,
                                    !string.IsNullOrEmpty(imageAttr?.Value) ? imageAttr.Value : null,
                                    null,
                                    "preimage",
                                    CrmPluginImageType.PreImage,
                                    CrmMessage.Instance[pluginStepAttr.Attribute("MessageName").Value]);
                            image.Name = "preimage";
                        }

                        if (p.Attribute("ImageType").Value == "PostImage")
                        {
                            image =
                                new CrmPluginImage(
                                    step.AssemblyId,
                                    step.PluginId,
                                    step.Id,
                                    !string.IsNullOrEmpty(imageAttr?.Value) ? imageAttr.Value : null,
                                    null,
                                    "postimage",
                                    CrmPluginImageType.PostImage,
                                    CrmMessage.Instance[pluginStepAttr.Attribute("MessageName").Value]);
                            image.Name = "postimage";
                        }

                        if (p.Attribute("ImageType").Value == "Both")
                        {
                            // ToDo: both
                        }

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
    }
}
