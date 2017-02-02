using System;

using Microsoft.Xrm.Sdk;

namespace PluginRegistrator.Entities
{
    public interface ICrmEntity
    {
        string EntityLogicalName { get; }

        Guid Id { get; }

        TEntity ToEntity<TEntity>() where TEntity : Entity;
    }
}
