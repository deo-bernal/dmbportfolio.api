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
        CreateMap<User, AdminUserDto>()
            .ForMember(
                destination => destination.LinkedProviders,
                options => options.MapFrom(source =>
                    source.ExternalLogins
                        .Select(login => login.Provider)
                        .OrderBy(provider => provider)
                        .ToList()));

        CreateMap<UserDetails, UserDetailsDto>()
            .ForMember(destination => destination.User, options => options.Ignore());

        CreateMap<ProjectType, ProjectTypeDto>();

        CreateMap<Project, ProjectDto>()
            .ForMember(destination => destination.User, options => options.Ignore());
    }
}
