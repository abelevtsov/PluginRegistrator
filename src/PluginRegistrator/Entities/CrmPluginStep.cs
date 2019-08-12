using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

using CrmSdk;
using Microsoft.Xrm.Sdk;

namespace PluginRegistrator.Entities
{
    public sealed class CrmPluginStep : ICrmEntity
    {
        public const string RelationshipStepToSecureConfig = "sdkmessageprocessingstepsecureconfigid_sdkmessageprocessingstep";

        public const string RelationshipStepToImage = "sdkmessageprocessingstepid_sdkmessageprocessingstepimage";

        private Guid assemblyId;

        private Guid pluginId;

        private Guid id;

        public CrmPluginStep()
        {
            Stage = CrmPluginStepStage.PostOperation;
            Deployment = CrmPluginStepDeployment.ServerOnly;
            Images = new List<CrmPluginImage>();
        }

        public CrmPluginStep(Guid assemblyId, SdkMessageProcessingStep step)
            : this()
        {
            RefreshFromSdkMessageProcessingStep(assemblyId, step);
        }

        public Guid Id
        {
            get => id;

            set
            {
                if (value == id)
                {
                    return;
                }

                id = value;

                foreach (var image in Images)
                {
                    image.StepId = value;
                }
            }
        }

        public Guid AssemblyId
        {
            get => assemblyId;

            set
            {
                if (value == assemblyId)
                {
                    return;
                }

                assemblyId = value;

                if (Images == null)
                {
                    return;
                }

                foreach (var image in Images)
                {
                    image.AssemblyId = value;
                }
            }
        }

        public Guid PluginId
        {
            get => pluginId;

            set
            {
                if (value == pluginId)
                {
                    return;
                }

                pluginId = value;

                if (Images == null)
                {
                    return;
                }

                foreach (var image in Images)
                {
                    image.PluginId = value;
                }
            }
        }

        public EntityReference EventHandler { get; set; }

        public Guid ServiceBusConfigurationId { get; set; }

        public bool Enabled { get; set; }

        public CrmPluginStepMode Mode { get; set; }

        public CrmPluginStepStage Stage { get; set; }

        public Guid MessageId { get; set; }

        public int Rank { get; set; }

        public string FilteringAttributes { get; set; }

        public CrmPluginStepDeployment Deployment { get; set; }

        public CrmPluginStepInvocationSource? InvocationSource { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string UnsecureConfiguration { get; set; }

        public Guid SecureConfigurationId { get; set; }

        public string SecureConfiguration { get; set; }

        public Guid ImpersonatingUserId { get; set; }

        public Guid MessageEntityId { get; set; }

        public bool DeleteAsyncOperationIfSuccessful { get; set; }

        public List<CrmPluginImage> Images { get; }

        public string EntityLogicalName => SdkMessageProcessingStep.EntityLogicalName;

        public void AddImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            Contract.EndContractBlock();

            Images.Add(image);
        }

        public TEntity ToEntity<TEntity>() where TEntity : Entity
        {
            var sdkStep =
                new SdkMessageProcessingStep
                    {
                        Configuration = UnsecureConfiguration,
                        EventHandler = ServiceBusConfigurationId == Guid.Empty
                            ? new EntityReference(PluginType.EntityLogicalName, PluginId)
                            : new EntityReference(ServiceEndpoint.EntityLogicalName, ServiceBusConfigurationId),
                        Name = Name,
                        Mode = new OptionSetValue((int)Mode),
                        Rank = Rank,
                        SdkMessageId = MessageId == Guid.Empty ? null : new EntityReference(SdkMessage.EntityLogicalName, MessageId),
                        SdkMessageFilterId = MessageEntityId == Guid.Empty ? null : new EntityReference(SdkMessageFilter.EntityLogicalName, MessageEntityId),
                        ImpersonatingUserId = ImpersonatingUserId == Guid.Empty ? null : new EntityReference("systemuser", ImpersonatingUserId),
                        Stage = new OptionSetValue((int)Stage),
                        SupportedDeployment = new OptionSetValue((int)Deployment),
                        FilteringAttributes = string.IsNullOrEmpty(FilteringAttributes) ? string.Empty : FilteringAttributes,
                        AsyncAutoDelete = DeleteAsyncOperationIfSuccessful,
                        Description = Description
                    };

            if (Id != Guid.Empty)
            {
                sdkStep.Id = Id;
            }

            if (InvocationSource != null)
            {
                sdkStep.InvocationSource = new OptionSetValue((int)InvocationSource);
            }

            return sdkStep.ToEntity<TEntity>();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CrmPluginStep other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id &&
                   AssemblyId == other.AssemblyId &&
                   PluginId == other.PluginId &&
                   //(EventHandler != null && EventHandler.Equals(other.EventHandler)) &&
                   ServiceBusConfigurationId == other.ServiceBusConfigurationId &&
                   Name == other.Name &&
                   Enabled == other.Enabled &&
                   Mode == other.Mode &&
                   Stage == other.Stage &&
                   MessageId == other.MessageId &&
                   Rank == other.Rank &&
                   FilteringAttributes == other.FilteringAttributes &&
                   Deployment == other.Deployment &&
                   //InvocationSource == other.InvocationSource &&
                   Name == other.Name &&
                   Description == other.Description &&
                   UnsecureConfiguration == other.UnsecureConfiguration &&
                   SecureConfigurationId == other.SecureConfigurationId &&
                   ImpersonatingUserId == other.ImpersonatingUserId &&
                   MessageEntityId == other.MessageEntityId &&
                   DeleteAsyncOperationIfSuccessful == other.DeleteAsyncOperationIfSuccessful;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^
                   AssemblyId.GetHashCode() ^
                   PluginId.GetHashCode() ^
                   EventHandler.GetHashCode() ^
                   ServiceBusConfigurationId.GetHashCode() ^
                   Name.GetHashCode() ^
                   Enabled.GetHashCode() ^
                   Mode.GetHashCode() ^
                   Stage.GetHashCode() ^
                   MessageId.GetHashCode() ^
                   Rank.GetHashCode() ^
                   FilteringAttributes.GetHashCode() ^
                   Deployment.GetHashCode() ^
                   InvocationSource.GetHashCode() ^
                   Name.GetHashCode() ^
                   Description.GetHashCode() ^
                   UnsecureConfiguration.GetHashCode() ^
                   SecureConfigurationId.GetHashCode() ^
                   ImpersonatingUserId.GetHashCode() ^
                   MessageEntityId.GetHashCode() ^
                   DeleteAsyncOperationIfSuccessful.GetHashCode();
        }

        private void RefreshFromSdkMessageProcessingStep(Guid pluginAssemblyId, SdkMessageProcessingStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            Contract.EndContractBlock();

            if (step.SupportedDeployment != null)
            {
                Deployment = (CrmPluginStepDeployment)step.SupportedDeployment.Value;
            }

            if (step.StateCode != null)
            {
                Enabled = step.StateCode.Value == SdkMessageProcessingStepState.On;
            }

            if (step.ImpersonatingUserId != null)
            {
                ImpersonatingUserId = step.ImpersonatingUserId.Id;
            }

            if (step.InvocationSource != null)
            {
                InvocationSource = (CrmPluginStepInvocationSource)step.InvocationSource.Value;
            }

            if (step.SdkMessageFilterId != null)
            {
                MessageEntityId = step.SdkMessageFilterId.Id;
            }

            if (step.SdkMessageId != null)
            {
                MessageId = step.SdkMessageId.Id;
            }

            if (step.Mode != null)
            {
                Mode = (CrmPluginStepMode)step.Mode.Value;
            }

            if (step.PluginTypeId != null)
            {
                PluginId = step.PluginTypeId.Id;
            }

            if (step.Rank != null)
            {
                Rank = step.Rank.Value;
            }

            SecureConfiguration = null;
            SecureConfigurationId = Guid.Empty;

            if (step.Stage != null)
            {
                Stage = (CrmPluginStepStage)step.Stage.Value;
            }

            AssemblyId = pluginAssemblyId;
            Id = step.Id;
            UnsecureConfiguration = step.Configuration;

            if (step.EventHandler != null)
            {
                EventHandler = step.EventHandler;
                if (EventHandler.LogicalName == ServiceEndpoint.EntityLogicalName)
                {
                    ServiceBusConfigurationId = step.EventHandler.Id;
                }
            }

            Name = step.Name;

            Description = step.Description;

            FilteringAttributes = step.FilteringAttributes;

            if (step.AsyncAutoDelete != null)
            {
                DeleteAsyncOperationIfSuccessful = (bool)step.AsyncAutoDelete;
            }
        }
    }
}
