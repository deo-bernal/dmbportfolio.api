using AutoMapper;
using Dmb.Data.Entities;
using Dmb.Model.Dtos;

namespace Dmb.Data.Mapper;

public class DmbDetailsMapperProfile : Profile
{
    public DmbDetailsMapperProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<User, UserCompleteDetailsDto>();

        CreateMap<UserDetails, UserDetailsDto>()
            .ForMember(destination => destination.User, options => options.Ignore());

        CreateMap<Project, ProjectDto>()
            .ForMember(destination => destination.User, options => options.Ignore());
    }
}
