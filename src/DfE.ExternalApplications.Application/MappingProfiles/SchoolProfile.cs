using AutoMapper;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Domain.Entities.Schools;

namespace DfE.ExternalApplications.Application.MappingProfiles
{
    public class SchoolProfile : Profile
    {
        public SchoolProfile()
        {
            CreateMap<School, Principal>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PrincipalId.Value))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.NameDetails.NameListAs!.Split(",", StringSplitOptions.None)[1].Trim()))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.NameDetails.NameListAs!.Split(",", StringSplitOptions.None)[0].Trim()))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.PrincipalDetails.Email))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.NameDetails.NameDisplayAs))
                .ForMember(dest => dest.DisplayNameWithTitle, opt => opt.MapFrom(src => src.NameDetails.NameFullTitle))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => new List<string> { "Student" }))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.LastRefresh))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PrincipalDetails.Phone))
                .ForMember(dest => dest.SchoolName, opt => opt.MapFrom(src => src.SchoolName));
        }
    }
}
