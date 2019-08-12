using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

using CrmSdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PluginRegistrator.Entities;

namespace PluginRegistrator.Helpers
{
    public static class RegistrationHelper
    {
        public static IOrganizationService OrgService { private get; set; }

        public static string GenerateStepName(string typeName, string messageName, string primaryEntityName, string secondaryEntityName)
        {
            primaryEntityName = primaryEntityName.ToLowerInvariant();
            if (primaryEntityName == "entity")
            {
                primaryEntityName = null;
            }

            var nameBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                nameBuilder.AppendFormat("{0}: ", typeName.Split('.').Last());
            }

            if (string.IsNullOrEmpty(messageName))
            {
                nameBuilder.Append("Not Specified of ");
            }
            else
            {
                nameBuilder.AppendFormat("{0} of ", messageName);
            }

            var hasPrimaryEntity = false;
            if (!string.IsNullOrEmpty(primaryEntityName) &&
                !string.Equals(primaryEntityName, "none", StringComparison.InvariantCultureIgnoreCase))
            {
                hasPrimaryEntity = true;
                nameBuilder.Append(primaryEntityName);
            }

            if (!(string.IsNullOrEmpty(secondaryEntityName) ||
                  string.Equals(secondaryEntityName, "none", StringComparison.InvariantCultureIgnoreCase)))
            {
                var format = hasPrimaryEntity ? "and {0}" : "{0}";

                nameBuilder.AppendFormat(format, secondaryEntityName);
            }
            else if (!hasPrimaryEntity)
            {
                nameBuilder.Append("any Entity");
            }

            return nameBuilder.ToString();
        }

        public static Guid RegisterServiceEndpoint(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException(nameof(serviceEndpoint));
            }

            Contract.EndContractBlock();

            var sdkServiceEndpoint = serviceEndpoint.ToEntity<ServiceEndpoint>();

            sdkServiceEndpoint.Id = OrgService.Create(sdkServiceEndpoint);
            serviceEndpoint.ServiceEndpointId = sdkServiceEndpoint.Id;

            return serviceEndpoint.Id;
        }

        public static Guid RegisterPlugin(CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            Contract.EndContractBlock();

            var pt = plugin.ToEntity<PluginType>();

            pt.Id = OrgService.Create(pt);
            plugin.Id = pt.Id;

            foreach (var step in plugin.Steps)
            {
                step.Id = RegisterStep(step);
            }

            return plugin.Id;
        }

        public static Guid RegisterStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            Contract.EndContractBlock();

            var sdkStep = step.ToEntity<SdkMessageProcessingStep>();

            sdkStep.Id = OrgService.Create(sdkStep);
            step.Id = sdkStep.Id;

            foreach (var image in step.Images)
            {
                image.Id = RegisterImage(image);
            }

            return step.Id;
        }

        public static Guid RegisterImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            Contract.EndContractBlock();

            var sdkImage = image.ToEntity<SdkMessageProcessingStepImage>();

            return OrgService.Create(sdkImage);
        }

        public static void UpdateServiceEndpoint(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException(nameof(serviceEndpoint));
            }

            Contract.EndContractBlock();

            var sep = serviceEndpoint.ToEntity<ServiceEndpoint>();

            OrgService.Update(sep);
        }

        public static void UpdatePlugin(CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            Contract.EndContractBlock();

            var pt = plugin.ToEntity<PluginType>();

            OrgService.Update(pt);
        }

        public static void UpdateStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            Contract.EndContractBlock();

            var sdkStep = step.ToEntity<SdkMessageProcessingStep>();

            OrgService.Update(sdkStep);

            UpdateStepStatus(step.Id, step.Enabled);
        }

        public static void UpdateStepStatus(Guid stepId, bool enabled)
        {
            if (stepId == Guid.Empty)
            {
                throw new ArgumentException("Invalid Guid", nameof(stepId));
            }

            Contract.EndContractBlock();

            var request =
                new SetStateRequest
                    {
                        EntityMoniker = new EntityReference(SdkMessageProcessingStep.EntityLogicalName, stepId),
                        State = new OptionSetValue(enabled ? (int)SdkMessageProcessingStepState.On : (int)SdkMessageProcessingStepState.Off),
                        Status = new OptionSetValue(-1)
                    };

            OrgService.Execute(request);
        }

        public static void UpdateImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            Contract.EndContractBlock();

            var sdkImage = image.ToEntity<SdkMessageProcessingStepImage>();

            OrgService.Update(sdkImage);
        }

        public static void Unregister(params ICrmEntity[] crmEntity)
        {
            if (crmEntity?.Length == 0)
            {
                throw new ArgumentNullException(nameof(crmEntity));
            }

            Contract.EndContractBlock();

            var serviceEndpointList = new List<Guid>();
            var assemblyList = new List<Guid>();
            var pluginList = new List<Guid>();
            var stepList = new List<Guid>();
            var imageList = new List<Guid>();

            foreach (var entity in crmEntity)
            {
                switch (entity.EntityLogicalName)
                {
                    case ServiceEndpoint.EntityLogicalName:
                        serviceEndpointList.Add(entity.Id);
                        break;
                    case PluginAssembly.EntityLogicalName:
                        assemblyList.Add(entity.Id);
                        break;
                    case PluginType.EntityLogicalName:
                        pluginList.Add(entity.Id);
                        break;
                    case SdkMessageProcessingStep.EntityLogicalName:
                        stepList.Add(entity.Id);
                        break;
                    case SdkMessageProcessingStepImage.EntityLogicalName:
                        imageList.Add(entity.Id);
                        break;
                    default:
                        throw new NotImplementedException("Type = " + entity.EntityLogicalName);
                }
            }

            foreach (var stepId in RetrieveStepIdsForServiceEndpoint(serviceEndpointList).Where(stepId => !stepList.Contains(stepId)))
            {
                stepList.Add(stepId);
            }

            foreach (var pluginId in RetrievePluginIdsForAssembly(assemblyList).Where(pluginId => !pluginList.Contains(pluginId)))
            {
                pluginList.Add(pluginId);
            }

            foreach (var stepId in RetrieveStepIdsForPlugins(pluginList).Where(stepId => !stepList.Contains(stepId)))
            {
                stepList.Add(stepId);
            }

            foreach (var imageId in RetrieveImageIdsForStepId(stepList).Where(imageId => !imageList.Contains(imageId)))
            {
                imageList.Add(imageId);
            }

            foreach (var imageId in imageList)
            {
                OrgService.Delete(SdkMessageProcessingStepImage.EntityLogicalName, imageId);
            }

            foreach (var stepId in stepList)
            {
                OrgService.Delete(SdkMessageProcessingStep.EntityLogicalName, stepId);
            }

            foreach (var pluginId in pluginList)
            {
                OrgService.Delete(PluginType.EntityLogicalName, pluginId);
            }

            foreach (var assemblyId in assemblyList)
            {
                OrgService.Delete(PluginAssembly.EntityLogicalName, assemblyId);
            }

            foreach (var serviceEndpointId in serviceEndpointList)
            {
                OrgService.Delete(ServiceEndpoint.EntityLogicalName, serviceEndpointId);
            }
        }

        public static IEnumerable<SdkMessage> RetrieveMessages()
        {
            var query =
                new QueryExpression(SdkMessage.EntityLogicalName)
                    {
                        ColumnSet = new ColumnSet("sdkmessageid", "createdon", "modifiedon", "name", "customizationlevel")
                    };
            query.Criteria.AddCondition("isprivate", ConditionOperator.Equal, false);
            query.AddOrder("name", OrderType.Ascending);

            return OrgService.RetrieveMultiple(query).Entities.Cast<SdkMessage>();
        }

        public static IEnumerable<SdkMessageFilter> RetrieveMessageFilters(params Guid[] messageIds)
        {
            var query =
                new QueryExpression(SdkMessageFilter.EntityLogicalName)
                    {
                        ColumnSet = new ColumnSet("sdkmessageid", "primaryobjecttypecode", "secondaryobjecttypecode", "customizationlevel", "availability")
                    };
            query.Criteria.AddCondition("sdkmessageid", ConditionOperator.In, messageIds.Cast<object>().ToArray());
            query.Criteria.AddCondition("iscustomprocessingstepallowed", ConditionOperator.Equal, true);
            query.Criteria.AddCondition("isvisible", ConditionOperator.Equal, true);

            return OrgService.RetrieveMultiple(query).Entities.Cast<SdkMessageFilter>();
        }

        private static IEnumerable<Guid> RetrieveStepIdsForServiceEndpoint(ICollection<Guid> serviceEndpointIds)
        {
            return RetrieveReferenceAttributeIds(SdkMessageProcessingStep.EntityLogicalName, "sdkmessageprocessingstepid", "eventhandler", serviceEndpointIds);
        }

        private static IEnumerable<Guid> RetrieveStepIdsForPlugins(ICollection<Guid> pluginIds)
        {
            return RetrieveReferenceAttributeIds(SdkMessageProcessingStep.EntityLogicalName, "sdkmessageprocessingstepid", "plugintypeid", pluginIds);
        }

        private static IEnumerable<Guid> RetrieveImageIdsForStepId(ICollection<Guid> stepIds)
        {
            return RetrieveReferenceAttributeIds(SdkMessageProcessingStepImage.EntityLogicalName, "sdkmessageprocessingstepimageid", "sdkmessageprocessingstepid", stepIds);
        }

        private static IEnumerable<Guid> RetrievePluginIdsForAssembly(ICollection<Guid> assemblyIds)
        {
            return RetrieveReferenceAttributeIds(PluginType.EntityLogicalName, "plugintypeid", "pluginassemblyid", assemblyIds);
        }

        private static IEnumerable<Guid> RetrieveReferenceAttributeIds(string entityName, string retrieveAttribute, string filterAttribute, ICollection<Guid> filterIds)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                throw new ArgumentNullException(nameof(entityName));
            }

            if (string.IsNullOrEmpty(retrieveAttribute))
            {
                throw new ArgumentNullException(nameof(retrieveAttribute));
            }

            if (string.IsNullOrEmpty(filterAttribute))
            {
                throw new ArgumentNullException(nameof(filterAttribute));
            }

            if (retrieveAttribute == filterAttribute)
            {
                throw new ArgumentException("Attributes must be different");
            }

            Contract.EndContractBlock();

            if (!filterIds.Any())
            {
                return Enumerable.Empty<Guid>();
            }

            var result = new List<Guid>();
            var query = new QueryExpression(entityName)
                            {
                                ColumnSet = new ColumnSet(retrieveAttribute)
                            };
            query.Criteria.AddCondition(filterAttribute, ConditionOperator.In, filterIds.Where(id => id != Guid.Empty).Cast<object>().ToArray());

            foreach (var value in OrgService.RetrieveMultiple(query).Entities.Select(e => e[retrieveAttribute]))
            {
                switch (value)
                {
                    case Guid guid:
                        result.Add(guid);
                        break;
                    case EntityReference entityRef:
                        result.Add(entityRef.Id);
                        break;
                }
            }

            return result;
        }
    }
}
