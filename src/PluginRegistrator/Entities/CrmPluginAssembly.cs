using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

using CrmSdk;
using Microsoft.Xrm.Sdk;

namespace PluginRegistrator.Entities
{
    public sealed class CrmPluginAssembly : ICrmEntity
    {
        private readonly List<CrmPlugin> crmPlugins = new List<CrmPlugin>();

        private string name;

        private Guid assemblyId;

        public CrmPluginAssembly()
        {
        }

        public CrmPluginAssembly(PluginAssembly assembly)
            : this()
        {
            RefreshFromPluginAssembly(assembly);
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value == name)
                {
                    return;
                }

                name = value;

                if (crmPlugins != null)
                {
                    foreach (var plugin in crmPlugins)
                    {
                        plugin.AssemblyName = value;
                    }
                }
            }
        }

        public Guid AssemblyId
        {
            get
            {
                return assemblyId;
            }

            set
            {
                if (value == assemblyId)
                {
                    return;
                }

                assemblyId = value;

                if (crmPlugins != null)
                {
                    foreach (var plugin in crmPlugins)
                    {
                        plugin.AssemblyId = value;
                    }
                }
            }
        }

        public CrmAssemblyIsolationMode IsolationMode { get; set; }

        public CrmAssemblySourceType SourceType { get; set; }

        public string ServerFileName { get; set; }

        public string Version { get; set; }

        public string PublicKeyToken { get; set; }

        public string Culture { get; set; }

        public string Description { get; set; }

        public Version SdkVersion { get; set; }

        public IEnumerable<CrmPlugin> CrmPlugins
        {
            get { return crmPlugins; }
        }

        public string EntityLogicalName
        {
            get
            {
                return PluginAssembly.EntityLogicalName;
            }
        }

        public Guid Id
        {
            get
            {
                return AssemblyId;
            }
        }

        public void AddPlugin(CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            Contract.EndContractBlock();

            crmPlugins.Add(plugin);
        }

        public TEntity ToEntity<TEntity>() where TEntity : Entity
        {
            var assembly =
                new PluginAssembly
                    {
                        SourceType = new OptionSetValue((int)SourceType),
                        IsolationMode = new OptionSetValue((int)IsolationMode),
                        Culture = Culture,
                        PublicKeyToken = PublicKeyToken,
                        Version = Version,
                        Name = Name,
                        Description = Description
                    };
            if (Id != Guid.Empty)
            {
                assembly.Id = Id;
            }

            return assembly.ToEntity<TEntity>();
        }

        private void RefreshFromPluginAssembly(PluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Contract.EndContractBlock();

            Name = assembly.Name;
            ServerFileName = assembly.Path;
            Version = assembly.Version;
            PublicKeyToken = assembly.PublicKeyToken;
            Culture = assembly.Culture;
            AssemblyId = assembly.Id;

            if (assembly.SourceType != null)
            {
                SourceType = (CrmAssemblySourceType)assembly.SourceType.Value;
            }

            if (assembly.IsolationMode != null)
            {
                IsolationMode = (CrmAssemblyIsolationMode)assembly.IsolationMode.Value;
            }

            Description = assembly.Description;
        }
    }
}
