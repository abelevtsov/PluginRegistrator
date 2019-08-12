using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CrmSdk;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PluginRegistrator.Entities;
using PluginRegistrator.Extensions;

namespace PluginRegistrator.Helpers
{
    public class AssemblyHelper
    {
        private IEnumerable<SdkMessage> messages;

        private IEnumerable<SdkMessageFilter> messageFilters;

        internal AssemblyHelper(IOrganizationService orgService, RegistrationHelper registrationHelper) =>
            (OrgService, RegistrationHelper) = (orgService, registrationHelper);

        private IOrganizationService OrgService { get; }

        private RegistrationHelper RegistrationHelper { get; }

        private IEnumerable<SdkMessage> Messages => messages ?? (messages = RegistrationHelper.RetrieveMessages());

        private IEnumerable<SdkMessageFilter> MessageFilters => messageFilters ?? (messageFilters = RegistrationHelper.RetrieveMessageFilters(Messages.Select(m => m.Id).ToArray()));

        public CrmPluginAssembly RetrievePluginsFromAssembly(string path, string pathToUnsecureConfigFile)
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
            pluginAssembly.FillPluginsFromAssembly(assembly, unsecureConfigItems, Messages, MessageFilters);

            return pluginAssembly;
        }

        public CrmPluginAssembly RetrievePluginsFromAssembly(string path, XDocument config)
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

            pluginAssembly.FillPluginsFromAssembly(sdkVersion, config, Messages, MessageFilters);

            return pluginAssembly;
        }

        public CrmPluginAssembly LoadAssemblyFromDB(string assemblyName)
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

        public void UpdateAssemblyInDB(string pathToAssembly, ICrmEntity assembly, params PluginType[] workflows)
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

            if (workflows?.Length > 0)
            {
                pluginAssembly.pluginassembly_plugintype = workflows;
            }

            OrgService.Update(pluginAssembly);
        }

        public Guid CreateAssemblyInDB(string pathToAssembly, ICrmEntity assembly)
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

        // ToDo: reflection only load from Stream
        private static Assembly LoadAssembly(string path) => Assembly.LoadFrom(path);
    }
}
