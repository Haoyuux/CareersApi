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

        // NEW: Refresh token methods
        Task<ApiResponseMessage<UserLoginDto>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponseMessage<bool>> InvalidateRefreshTokenAsync(string refreshToken);
        Task<ApiResponseMessage<bool>> LogoutAsync(string refreshToken);
        Task<ApiResponseMessage<string>> InsertOrUpdateUserProfile(InsertOrUpdateUserProfileDto input);
    }
}