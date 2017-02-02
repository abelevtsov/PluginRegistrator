namespace PluginRegistrator.Entities
{
    public enum CrmPluginStepMode
    {
        Asynchronous = 1,
        Synchronous = 0
    }

    public enum CrmPluginStepStage
    {
        PreValidation = 10,
        PreOperation = 20,
        PostOperation = 40,
        PostOperationDeprecated = 50
    }

    public enum CrmPluginStepDeployment
    {
        ServerOnly = 0,
        OfflineOnly = 1,
        Both = 2
    }

    public enum CrmPluginStepInvocationSource
    {
        Parent = 0,
        Child = 1
    }

    public enum CrmPluginType
    {
        Plugin,
        WorkflowActivity
    }

    public enum CrmPluginIsolatable
    {
        Yes,
        No,
        Unknown
    }

    public enum CrmPluginImageType
    {
        PreImage = 0,
        PostImage = 1,
        Both = 2
    }

    public enum CrmServiceEndpointContract
    {
        OneWay = 1,
        Queue = 2,
        Rest = 3,
        TwoWay = 4
    }

    public enum CrmServiceEndpointUserClaim
    {
        None = 1,
        UserId = 2,
        UserInfo = 3
    }

    public enum CrmServiceEndpointConnectionMode
    {
        Normal = 1,
        Federated = 2
    }
}
