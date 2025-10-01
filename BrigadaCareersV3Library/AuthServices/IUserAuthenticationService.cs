using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;
using static BrigadaCareersV3Library.AuthServices.UserAuthenticationService;

namespace BrigadaCareersV3Library.AuthServices
{
    public interface IUserAuthenticationService
    {
        Task<string> RegisteredUser(UserDto register);
        Task<string> CreateUser(UserDto register);
        Task<string> RegisteredAdmin(RegisterUserDto register);
        Task<ApiResponseMessage<UserLoginDto>> LoginAccount(RegisterUserDto login);
        Task<ApiResponseMessage<UserDto>> getUserProfileDetails();
        Task<ApiResponseMessage<UserLoginDto>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponseMessage<bool>> InvalidateRefreshTokenAsync(string refreshToken);
        Task<ApiResponseMessage<bool>> LogoutAsync(string refreshToken);
        Task<ApiResponseMessage<string>> InsertOrUpdateUserProfile(InsertOrUpdateUserProfileDto input);
        //EDUCATION
        Task<ApiResponseMessage<string>> CreateOrEditEducation(CreateOrEditEducationDto input);
        Task<ApiResponseMessage<IList<CreateOrEditEducationDto>>> GetUserEducation();
        Task<ApiResponseMessage<string>> DeleteUserEducation(Guid educationId);
        //WORK EXPERIENCE
        Task<ApiResponseMessage<string>> CreateOrEditWorkExperience(CreateOrEditWorkExperienceDto input);
        Task<ApiResponseMessage<IList<CreateOrEditWorkExperienceDto>>> GetUserWorkExperience();
        Task<ApiResponseMessage<string>> DeleteUserWorkExperience(Guid workexperienceId);
    }
}