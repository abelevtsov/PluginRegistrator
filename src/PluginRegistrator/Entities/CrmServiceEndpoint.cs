using System;
using System.Collections.Generic;

using CrmSdk;
using Microsoft.Xrm.Sdk;

namespace PluginRegistrator.Entities
{
    public sealed class CrmServiceEndpoint : ICrmEntity
    {
        public const string ServiceBusPluginName = "Microsoft.Crm.ServiceBus.ServiceBusPlugin";

        public const string ServiceBusPluginAssemblyName = "Microsoft.Crm.ServiceBus";

        public readonly Guid ServiceBusPluginId = Guid.Parse("EF521E63-CD2B-4170-99F6-447466A7161E");

        public readonly Guid ServiceBusPluginAssemblyId = Guid.Parse("A430B185-D19D-428C-B156-5EBE3F391564");

        public CrmServiceEndpoint(ServiceEndpoint serviceEndpoint)
        {
            RefreshFromServiceEndpoint(serviceEndpoint);
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string SolutionNamespace { get; set; }

        public string Path { get; set; }

        public CrmServiceEndpointContract Contract { get; set; }

        public CrmServiceEndpointUserClaim UserClaim { get; set; }

        public CrmServiceEndpointConnectionMode ConnectionMode { get; set; }

        public Guid ServiceEndpointId { get; set; }

        public Guid PluginId
        {
            get { return ServiceBusPluginId; }
        }

        public string AcsManagementKey { get; set; }

        public string AcsPublicCertificate { get; set; }

        public string AcsIssuerName { get; set; }

        public string EntityLogicalName
        {
            get { return ServiceEndpoint.EntityLogicalName; }
        }

        public Guid Id
        {
            get { return ServiceEndpointId; }
        }

        public IEnumerable<CrmPluginStep> Steps
        {
            get
            {
                var steps = new Dictionary<Guid, CrmPluginStep>();

                //// ToDo: fix steps
                ////foreach (CrmPluginStep step in m_org.Steps.Values)
                ////{
                ////    if (step.ServiceBusConfigurationId == ServiceEndpointId)
                ////    {
                ////        steps.Add(step.StepId, step);
                ////    }
                ////}

                return new List<CrmPluginStep>(steps.Values);
            }
        }

        public TEntity ToEntity<TEntity>() where TEntity : Entity
        {
            var serviceEndPoint =
                new ServiceEndpoint
                    {
                        ConnectionMode = new OptionSetValue((int)ConnectionMode),
                        Contract = new OptionSetValue((int)Contract),
                        UserClaim = new OptionSetValue((int)UserClaim),
                        Description = Description,
                        Name = Name,
                        Path = Path,
                        SolutionNamespace = SolutionNamespace
                    };
            if (Id != Guid.Empty)
            {
                serviceEndPoint.Id = Id;
            }

            return serviceEndPoint.ToEntity<TEntity>();
        }

        private void RefreshFromServiceEndpoint(ServiceEndpoint serviceEndPoint)
        {
            if (serviceEndPoint == null)
            {
                throw new ArgumentNullException(nameof(serviceEndPoint));
            }

            Name = serviceEndPoint.Name;
            Description = serviceEndPoint.Description;
            Path = serviceEndPoint.Path;
            SolutionNamespace = serviceEndPoint.SolutionNamespace;

            if (serviceEndPoint.ServiceEndpointId != Guid.Empty)
            {
                ServiceEndpointId = serviceEndPoint.ServiceEndpointId.Value;
            }

            if (serviceEndPoint.ConnectionMode != null)
            {
                ConnectionMode = (CrmServiceEndpointConnectionMode)serviceEndPoint.ConnectionMode.Value;
            }

            if (serviceEndPoint.Contract != null)
            {
                Contract = (CrmServiceEndpointContract)serviceEndPoint.Contract.Value;
            }

            if (serviceEndPoint.UserClaim != null)
            {
                UserClaim = (CrmServiceEndpointUserClaim)serviceEndPoint.UserClaim.Value;
            }
        }
    }
}
