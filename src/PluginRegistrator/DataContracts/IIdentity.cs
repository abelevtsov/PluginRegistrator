namespace PluginRegistrator.DataContracts
{
    public interface IIdentity<TKey> where TKey : struct
    {
        TKey Id { get; set; }
    }
}
