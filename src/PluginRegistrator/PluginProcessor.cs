using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using CrmSdk;
using Microsoft.Xrm.Sdk;
using PluginRegistrator.Entities;

namespace PluginRegistrator
{
    public class PluginProcessor
    {
        public PluginProcessor(IOrganizationService orgService)
        {
            OrgService = orgService;
            AssemblyReader.OrgService = OrgService;
            RegistrationHelper.OrgService = OrgService;
        }

        private IOrganizationService OrgService { get; }

        public void Process(string pluginsPath, string mergedPluginAssemblyPath, string unsecureConfigFilePath)
        {
            var assembly = LoadAssemblyFromDisk(pluginsPath, unsecureConfigFilePath);
            Process(assembly, mergedPluginAssemblyPath);
        }

        public void Process(string mergedPluginAssemblyPath, string configPath)
        {
            var config = XDocument.Load(configPath);
            var assembly = LoadAssemblyFromDisk(mergedPluginAssemblyPath, config);
            Process(assembly, mergedPluginAssemblyPath);
        }

        private static void Process(CrmPluginAssembly assembly, string mergedPluginAssemblyPath)
        {
            // ToDo: use DataFlow
            var currentAssembly = LoadAssemblyFromDB(assembly.Name);
            var createAssembly = currentAssembly == null;
            var pluginsForRegister = new List<CrmPlugin>();
            var pluginsForRemove = new List<CrmPlugin>();
            var pluginsForUpdate = new List<Tuple<CrmPlugin, CrmPlugin>>();

            if (createAssembly)
            {
                pluginsForRegister.AddRange(assembly.CrmPlugins);
            }
            else
            {
                Func<CrmPluginAssembly, CrmPlugin, CrmPlugin> getCorrelated = (a, plugin) => a.CrmPlugins.FirstOrDefault(p => p.TypeName == plugin.TypeName);
                pluginsForRegister.AddRange(from plugin in assembly.CrmPlugins
                                            where getCorrelated(currentAssembly, plugin) == null
                                            select plugin);

                pluginsForRemove.AddRange(from plugin in currentAssembly.CrmPlugins
                                          where getCorrelated(assembly, plugin) == null
                                          select plugin);

                pluginsForUpdate.AddRange(from plugin in assembly.CrmPlugins
                                          let existed = getCorrelated(currentAssembly, plugin)
                                          where existed != null
                                          select new Tuple<CrmPlugin, CrmPlugin>(plugin, existed));
            }

            if (createAssembly)
            {
                try
                {
                    assembly.IsolationMode = CrmAssemblyIsolationMode.None;
                    assembly.AssemblyId = CreateAssemblyInDB(mergedPluginAssemblyPath, assembly);
                }
                catch (Exception ex)
                {
                    throw new Exception("ERROR: Error occurred while registering the plugin assembly", ex);
                }
            }
            else
            {
                UnregisterPlugins(pluginsForRemove);

                assembly.IsolationMode = currentAssembly.IsolationMode;
                assembly.AssemblyId = currentAssembly.AssemblyId;

                var workflows =
                        (from plugin in currentAssembly.CrmPlugins
                         where plugin.PluginType == CrmPluginType.WorkflowActivity
                         select
                             new PluginType
                                 {
                                     Id = plugin.Id,
                                     WorkflowActivityGroupName = plugin.WorkflowActivityGroupName
                                 }).ToArray();

                UpdateAssemblyInDB(mergedPluginAssemblyPath, assembly, workflows);
            }

            RegisterPlugins(pluginsForRegister);

            UpdatePlugins(pluginsForUpdate);
        }

        private static void UnregisterPlugins(IEnumerable<CrmPlugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                RegistrationHelper.Unregister(plugin);
            }
        }

        private static void RegisterPlugins(IEnumerable<CrmPlugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                plugin.Id = RegistrationHelper.RegisterPlugin(plugin);
            }
        }

        private static void UpdatePlugins(IEnumerable<Tuple<CrmPlugin, CrmPlugin>> plugins)
        {
            foreach (var pair in plugins)
            {
                var plugin = pair.Item1;
                var currentPlugin = pair.Item2;
                plugin.Id = currentPlugin.Id;
                if (!plugin.Equals(currentPlugin))
                {
                    RegistrationHelper.UpdatePlugin(plugin);
                }

                foreach (var step in currentPlugin.Steps)
                {
                    var currentStep = plugin.Steps.SingleOrDefault(s => s.Name == step.Name);
                    if (currentStep == null)
                    {
                        RegistrationHelper.Unregister(step);
                    }
                }

                foreach (var step in plugin.Steps)
                {
                    var currentSteps = currentPlugin.Steps.Where(s => s.Name == step.Name).ToList();
                    if (!currentSteps.Any())
                    {
                        step.Id = RegistrationHelper.RegisterStep(step);
                    }
                    else
                    {
                        var currentStep = currentSteps.First();
                        step.Id = currentStep.Id;
                        step.ImpersonatingUserId = currentStep.ImpersonatingUserId;
                        if (!step.Equals(currentStep))
                        {
                            RegistrationHelper.UpdateStep(step);
                        }

                        foreach (var image in currentStep.Images)
                        {
                            var currentImage = step.Images.SingleOrDefault(i => i.Name == image.Name);
                            if (currentImage == null)
                            {
                                RegistrationHelper.Unregister(image);
                            }
                        }

                        foreach (var image in step.Images)
                        {
                            var currentImage = currentStep.Images.SingleOrDefault(i => i.Name == image.Name);
                            if (currentImage == null)
                            {
                                image.Id = RegistrationHelper.RegisterImage(image);
                            }
                            else
                            {
                                image.Id = currentImage.Id;
                                if (!image.Equals(currentImage))
                                {
                                    RegistrationHelper.UpdateImage(image);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static CrmPluginAssembly LoadAssemblyFromDisk(string path, string pathToUnsecureConfigFile) =>
            AssemblyReader.RetrievePluginsFromAssembly(path, pathToUnsecureConfigFile);

        private static CrmPluginAssembly LoadAssemblyFromDisk(string path, XDocument config) =>
            AssemblyReader.RetrievePluginsFromAssembly(path, config);

        private static CrmPluginAssembly LoadAssemblyFromDB(string assemblyName) =>
            AssemblyReader.LoadAssemblyFromDB(assemblyName);

        private static void UpdateAssemblyInDB(string pathToAssembly, ICrmEntity assembly, params PluginType[] workflows) =>
            AssemblyReader.UpdateAssemblyInDB(pathToAssembly, assembly, workflows);

        private static Guid CreateAssemblyInDB(string pathToAssembly, ICrmEntity assembly) =>
            AssemblyReader.CreateAssemblyInDB(pathToAssembly, assembly);
    }
}
