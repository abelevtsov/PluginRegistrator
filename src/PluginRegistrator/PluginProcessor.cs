using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using AutoMapper;
using CrmSdk;
using Microsoft.Xrm.Sdk;
using PluginRegistrator.Entities;
using PluginRegistrator.Helpers;
using PluginRegistrator.Profiles;

namespace PluginRegistrator
{
    public class PluginProcessor
    {
        public PluginProcessor(IOrganizationService orgService)
        {
            Mapper =
                new MapperConfiguration(
                    cfg =>
                    {
                        cfg.AddProfile<CrmPluginAssemblyProfile>();
                    }).CreateMapper();

            RegistrationHelper = new RegistrationHelper(orgService);
            AssemblyHelper = new AssemblyHelper(orgService, RegistrationHelper, Mapper);
        }

        private AssemblyHelper AssemblyHelper { get; }

        private RegistrationHelper RegistrationHelper { get; }

        private IMapper Mapper { get; }

        public void Process(string pluginsPath, string mergedPluginAssemblyPath, string unsecureConfigFilePath)
        {
            var assembly = AssemblyHelper.RetrievePluginsFromAssembly(pluginsPath, unsecureConfigFilePath);
            Process(assembly, mergedPluginAssemblyPath);
        }

        public void Process(string mergedPluginAssemblyPath, string configPath)
        {
            var config = XDocument.Load(configPath);
            var assembly = AssemblyHelper.RetrievePluginsFromAssembly(mergedPluginAssemblyPath, config);
            Process(assembly, mergedPluginAssemblyPath);
        }

        private void Process(CrmPluginAssembly assembly, string mergedPluginAssemblyPath)
        {
            // ToDo: use DataFlow
            var currentAssembly = AssemblyHelper.LoadAssemblyFromDB(assembly.Name);
            var createAssembly = currentAssembly == null;
            var pluginsForRegister = new List<CrmPlugin>();
            var pluginsForRemove = new List<CrmPlugin>();
            var pluginsForUpdate = new List<(CrmPlugin plugin, CrmPlugin existed)>();

            if (createAssembly)
            {
                pluginsForRegister.AddRange(assembly.CrmPlugins);
            }
            else
            {
                CrmPlugin GetCorrelated(CrmPluginAssembly a, CrmPlugin plugin) => a.CrmPlugins.FirstOrDefault(p => p.TypeName == plugin.TypeName);

                pluginsForRegister.AddRange(
                    from plugin in assembly.CrmPlugins
                    where GetCorrelated(currentAssembly, plugin) == null
                    select plugin);

                pluginsForRemove.AddRange(
                    from plugin in currentAssembly.CrmPlugins
                    where GetCorrelated(assembly, plugin) == null
                    select plugin);

                pluginsForUpdate.AddRange(
                    from plugin in assembly.CrmPlugins
                    let existed = GetCorrelated(currentAssembly, plugin)
                    where existed != null
                    select (plugin, existed));
            }

            if (createAssembly)
            {
                try
                {
                    assembly.IsolationMode = CrmAssemblyIsolationMode.None;
                    assembly.AssemblyId = AssemblyHelper.CreateAssemblyInDB(mergedPluginAssemblyPath, assembly);
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

                AssemblyHelper.UpdateAssemblyInDB(mergedPluginAssemblyPath, assembly, workflows);
            }

            RegisterPlugins(pluginsForRegister);

            UpdatePlugins(pluginsForUpdate);
        }

        private void UnregisterPlugins(IEnumerable<CrmPlugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                RegistrationHelper.Unregister(plugin);
            }
        }

        private void RegisterPlugins(IEnumerable<CrmPlugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                plugin.Id = RegistrationHelper.RegisterPlugin(plugin);
            }
        }

        private void UpdatePlugins(IEnumerable<(CrmPlugin, CrmPlugin)> plugins)
        {
            foreach (var (plugin, currentPlugin) in plugins)
            {
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
    }
}
