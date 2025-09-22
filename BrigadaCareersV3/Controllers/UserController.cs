using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.AuthServices;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrigadaCareersV3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserAuthenticationService _userAuthentication;

        public UserController(IUserAuthenticationService userAuthentication)
        {
            _userAuthentication = userAuthentication;
        }

        [HttpPost("RegisterUser")]
        public async Task<ActionResult<ApiResponseMessage<string>>> RegisterUser(UserDto user)
        {
            var result = await _userAuthentication.RegisteredUser(user);
            var _api = new ApiResponseMessage<string>
            {
                Data = result,
                IsSuccess = true,
                ErrorMessage = ""
            };
            return Ok(_api);
        }

        //[Authorize(Roles = UserRole.Admin)]
        [HttpPost("RegisterAdmin")]
        public async Task<ActionResult<ApiResponseMessage<string>>> RegisterAdmin(RegisterUserDto user)
        {
            var res = await _userAuthentication.RegisteredAdmin(user);
            if (res != null)
            {
                return Ok(res);
            }
            return BadRequest(res);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<ApiResponseMessage<UserLoginDto>>> Login(RegisterUserDto user)
        {
            var result = await _userAuthentication.LoginAccount(user);

            if (result.IsSuccess && result.Data != null)
            {
                // Set refresh token as HttpOnly cookie
                var refreshTokenCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production with HTTPS
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7), // Refresh token expires in 7 days
                    Path = "/"
                };

                Response.Cookies.Append("refreshToken", result.Data.newRefreshToken, refreshTokenCookieOptions);

                // Don't send refresh token in response body for security
                result.Data.newRefreshToken = null;
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponseMessage<UserLoginDto>>> RefreshToken()
        {
            // Get refresh token from HttpOnly cookie
            var refreshToken = Request.Cookies["refreshToken"];

            // Call service method
            var result = await _userAuthentication.RefreshTokenAsync(refreshToken);

            if (result.IsSuccess && result.Data != null)
            {
                // Update refresh token cookie with new token (token rotation)
                var refreshTokenCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production with HTTPS
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Path = "/"
                };

                Response.Cookies.Append("refreshToken", result.Data.newRefreshToken, refreshTokenCookieOptions);

                // Don't send refresh token in response
                result.Data.newRefreshToken = null;

                return Ok(result);
            }

            return Unauthorized(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponseMessage<bool>>> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            // Call service method
            var result = await _userAuthentication.LogoutAsync(refreshToken);

            if (result.IsSuccess)
            {
                // Clear refresh token cookie
                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production with HTTPS
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

                return Ok(result);
            }

            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("getUserProfileDetails")]
        public async Task<ActionResult<ApiResponseMessage<getUserProfileDetailsDto>>> getUserProfileDetails()
        {
            var result = await _userAuthentication.getUserProfileDetails();
            return Ok(result);
        }
    }
}