using System.Globalization;
using System.Linq;
using System.Reflection;

using AutoMapper;
using PluginRegistrator.Entities;

namespace PluginRegistrator.Profiles
{
    public class CrmPluginAssemblyProfile : Profile
    {
        public CrmPluginAssemblyProfile()
        {
            CreateMap<Assembly, CrmPluginAssembly>()
                .ForMember(dest => dest.SourceType, opts => opts.MapFrom(o => CrmAssemblySourceType.Database))
                .ForMember(dest => dest.Name, opts => opts.MapFrom(o => o.GetName().Name))
                .ForMember(dest => dest.Version, opts => opts.MapFrom(o => o.GetName().Version.ToString()))
                .ForMember(dest => dest.Culture, opts => opts.MapFrom(o => o.GetName().CultureInfo.LCID == CultureInfo.InvariantCulture.LCID ? "neutral" : o.GetName().CultureInfo.Name))
                .ForMember(dest => dest.PublicKeyToken, opts => opts.MapFrom(o => PublicKeyToken(o)));
        }

        private static string PublicKeyToken(Assembly assembly)
        {
            var tokenBytes = assembly.GetName().GetPublicKeyToken();
            return tokenBytes == null || tokenBytes.Length == 0
                ? null
                : string.Join(string.Empty, tokenBytes.Select(b => b.ToString("X2")));
        }
    }
}
