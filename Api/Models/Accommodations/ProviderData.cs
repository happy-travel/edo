using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct ProviderData<TData>
    {
        public ProviderData(DataProviders source, TData data)
        {
            Source = source;
            Data = data;
        }
        
        public DataProviders Source { get; }
        public TData Data { get; }
    }
    
    public static class ProviderData
    {
        public static ProviderData<TProviderData> Create<TProviderData>(DataProviders source, TProviderData data) => new ProviderData<TProviderData>(source, data);
    }
}