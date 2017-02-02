using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

using CrmSdk;
using Microsoft.Xrm.Sdk;

namespace PluginRegistrator.Entities
{
    public sealed class CrmPlugin : ICrmEntity
    {
        private readonly List<CrmPluginStep> steps = new List<CrmPluginStep>();

        private Guid pluginAssemblyId;

        private Guid id;

        public CrmPlugin()
        {
        }

        public CrmPlugin(PluginType type) : this()
        {
            RefreshFromPluginType(type);
        }

        public string AssemblyName { get; set; }

        public string TypeName { get; set; }

        public string Description { get; set; }

        public string FriendlyName { get; set; }

        public CrmPluginType PluginType { get; set; }

        public CrmPluginIsolatable Isolatable { get; set; }

        public string Name { get; set; }

        public string WorkflowActivityGroupName { get; set; }

        public Guid AssemblyId
        {
            get
            {
                return pluginAssemblyId;
            }

            set
            {
                if (value == pluginAssemblyId)
                {
                    return;
                }

                pluginAssemblyId = value;

                foreach (var step in steps)
                {
                    step.AssemblyId = value;
                }
            }
        }

        public IEnumerable<CrmPluginStep> Steps
        {
            get { return steps; }
        }

        public string EntityLogicalName
        {
            get
            {
                return CrmSdk.PluginType.EntityLogicalName;
            }
        }

        public Guid Id
        {
            get
            {
                return id;
            }

            set
            {
                if (value == id)
                {
                    return;
                }

                id = value;

                foreach (var step in steps)
                {
                    step.PluginId = value;
                }
            }
        }

        public void AddStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            steps.Add(step);
        }

        public TEntity ToEntity<TEntity>() where TEntity : Entity
        {
            if (AssemblyId == Guid.Empty)
            {
                throw new ArgumentException("PluginAssembly has not been set", "plugin");
            }

            Contract.EndContractBlock();

            var plugin =
                new PluginType
                    {
                        PluginAssemblyId = new EntityReference(PluginAssembly.EntityLogicalName, AssemblyId),
                        TypeName = TypeName,
                        FriendlyName = FriendlyName,
                        Name = Name,
                        Description = Description,
                        WorkflowActivityGroupName = WorkflowActivityGroupName
                    };
            if (Id != Guid.Empty)
            {
                plugin.Id = Id;
            }

            return plugin.ToEntity<TEntity>();
        }

        public override bool Equals(object obj)
        {
            var other = obj as CrmPlugin;
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id &&
                   AssemblyName == other.AssemblyName &&
                   TypeName == other.TypeName &&
                   Description == other.Description &&
                   FriendlyName == other.FriendlyName &&
                   PluginType == other.PluginType &&
                   Isolatable == other.Isolatable &&
                   Name == other.Name &&
                   WorkflowActivityGroupName == other.WorkflowActivityGroupName &&
                   AssemblyId == other.AssemblyId;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^
                   AssemblyName.GetHashCode() ^
                   TypeName.GetHashCode() ^
                   Description.GetHashCode() ^
                   FriendlyName.GetHashCode() ^
                   PluginType.GetHashCode() ^
                   Isolatable.GetHashCode() ^
                   Name.GetHashCode() ^
                   WorkflowActivityGroupName.GetHashCode() ^
                   AssemblyId.GetHashCode();
        }

        private void RefreshFromPluginType(PluginType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Contract.EndContractBlock();

            Id = type.Id;
            AssemblyName = type.AssemblyName;
            TypeName = type.TypeName;
            FriendlyName = type.FriendlyName;
            Name = type.Name;
            Description = type.Description;

            if (type.PluginAssemblyId != null)
            {
                AssemblyId = type.PluginAssemblyId.Id;
            }

            PluginType = type.IsWorkflowActivity.GetValueOrDefault() ? CrmPluginType.WorkflowActivity : CrmPluginType.Plugin;

            WorkflowActivityGroupName = type.WorkflowActivityGroupName;
        }
    }
}
