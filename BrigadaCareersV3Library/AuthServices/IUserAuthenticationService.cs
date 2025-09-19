using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;

namespace BrigadaCareersV3Library.AuthServices
{
    public interface IUserAuthenticationService
    {
        Task<ApiResponseMessage<UserLoginDto>> LoginAccount(RegisterUserDto login);
        Task<string> RegisteredAdmin(RegisterUserDto register);
        Task<string> RegisteredUser(UserDto register);
        Task<string> CreateUser(UserDto register);
        Task<string> UpdateUserDetails(UserDto register);
    }
}