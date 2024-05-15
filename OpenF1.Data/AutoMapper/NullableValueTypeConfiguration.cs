using AutoMapper;

namespace OpenF1.Data.AutoMapper;

public class NullableValueTypeConfiguration : Profile
{
    public NullableValueTypeConfiguration()
    {
        CreateMap<bool?, bool?>().ConvertUsing((src, dest) => src.HasValue ? src : dest);
        CreateMap<int?, int?>().ConvertUsing((src, dest) => src.HasValue ? src : dest);
    }
}
