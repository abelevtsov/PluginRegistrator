using System;
using System.Diagnostics.Contracts;

using CrmSdk;
using Microsoft.Xrm.Sdk;

namespace PluginRegistrator.Entities
{
    public sealed class CrmPluginImage : ICrmEntity
    {
        public CrmPluginImage(
            Guid assemblyId,
            Guid pluginId,
            Guid stepId,
            string attributes,
            string relatedAttribute,
            string entityAlias,
            CrmPluginImageType imageType,
            string messagePropertyName)
        {
            AssemblyId = assemblyId;
            PluginId = pluginId;
            StepId = stepId;
            Attributes = attributes;
            RelatedAttribute = relatedAttribute;
            EntityAlias = entityAlias;
            ImageType = imageType;
            MessagePropertyName = messagePropertyName;
        }

        public CrmPluginImage(Guid assemblyId, Guid pluginId, SdkMessageProcessingStepImage image)
        {
            RefreshFromSdkMessageProcessingStepImage(assemblyId, pluginId, image);
        }

        public Guid Id { get; set; }

        public Guid AssemblyId { get; set; }

        public Guid PluginId { get; set; }

        public Guid StepId { get; set; }

        public string Attributes { get; set; }

        public string Name { get; set; }

        public string RelatedAttribute { get; set; }

        public string EntityAlias { get; set; }

        public CrmPluginImageType ImageType { get; set; }

        public string MessagePropertyName { get; set; }

        public string EntityLogicalName => SdkMessageProcessingStepImage.EntityLogicalName;

        public TEntity ToEntity<TEntity>() where TEntity : Entity
        {
            var image =
                new SdkMessageProcessingStepImage
                    {
                        SdkMessageProcessingStepId = new EntityReference(SdkMessageProcessingStep.EntityLogicalName, StepId),
                        ImageType = new OptionSetValue((int)ImageType),
                        MessagePropertyName = MessagePropertyName,
                        Name = Name,
                        EntityAlias = EntityAlias,
                        Attributes = string.IsNullOrEmpty(Attributes) ? string.Empty : Attributes
                    };
            if (Id != Guid.Empty)
            {
                image.Id = Id;
            }

            if (!string.IsNullOrEmpty(RelatedAttribute))
            {
                image.RelatedAttributeName = RelatedAttribute;
            }

            return image.ToEntity<TEntity>();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CrmPluginImage other))
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
                   StepId == other.StepId &&
                   Attributes == other.Attributes &&
                   Name == other.Name &&
                   RelatedAttribute == other.RelatedAttribute &&
                   EntityAlias == other.EntityAlias &&
                   ImageType == other.ImageType &&
                   MessagePropertyName == other.MessagePropertyName;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^
                   AssemblyId.GetHashCode() ^
                   PluginId.GetHashCode() ^
                   StepId.GetHashCode() ^
                   Attributes.GetHashCode() ^
                   Name.GetHashCode() ^
                   RelatedAttribute.GetHashCode() ^
                   EntityAlias.GetHashCode() ^
                   ImageType.GetHashCode() ^
                   MessagePropertyName.GetHashCode();
        }

        private void RefreshFromSdkMessageProcessingStepImage(Guid assemblyId, Guid pluginId, SdkMessageProcessingStepImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            Contract.EndContractBlock();

            Id = image.Id;
            AssemblyId = assemblyId;
            PluginId = pluginId;
            Attributes = image.Attributes;
            EntityAlias = image.EntityAlias;
            MessagePropertyName = image.MessagePropertyName;
            RelatedAttribute = image.RelatedAttributeName;
            Name = image.Name;

            if (image.SdkMessageProcessingStepId != null)
            {
                StepId = image.SdkMessageProcessingStepId.Id;
            }

            if (image.ImageType != null)
            {
                ImageType = (CrmPluginImageType)image.ImageType.Value;
            }
        }
    }
}
