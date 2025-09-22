using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;

namespace BrigadaCareersV3Library.AuthServices
{
    public interface IUserAuthenticationService
    {
        Task<string> RegisteredUser(UserDto register);
        Task<string> CreateUser(UserDto register);
        Task<string> UpdateUserDetails(UserDto register);
        Task<string> RegisteredAdmin(RegisterUserDto register);
        Task<ApiResponseMessage<UserLoginDto>> LoginAccount(RegisterUserDto login);
        Task<ApiResponseMessage<getUserProfileDetailsDto>> getUserProfileDetails();
        string GetCurrentUserId();

        // NEW: Refresh token methods
        Task<ApiResponseMessage<UserLoginDto>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponseMessage<bool>> InvalidateRefreshTokenAsync(string refreshToken);
        Task<ApiResponseMessage<bool>> LogoutAsync(string refreshToken);
    }
}