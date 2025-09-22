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

        
        private static CookieOptions BuildRefreshCookieOptions() => new CookieOptions
        {
            HttpOnly = true,
            Secure = true,              
            SameSite = SameSiteMode.None, // allow cross-site
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/"
        };

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

        [HttpPost("RegisterAdmin")]
        public async Task<ActionResult<ApiResponseMessage<string>>> RegisterAdmin(RegisterUserDto user)
        {
            var res = await _userAuthentication.RegisteredAdmin(user);
            if (res != null) return Ok(res);
            return BadRequest(res);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<ApiResponseMessage<UserLoginDto>>> Login(RegisterUserDto user)
        {
            var result = await _userAuthentication.LoginAccount(user);

            if (result.IsSuccess && result.Data != null)
            {
                // Set refresh cookie with cross-site flags
                Response.Cookies.Append("refreshToken", result.Data.newRefreshToken, BuildRefreshCookieOptions());

                // Never return the refresh token in the body
                result.Data.newRefreshToken = null;
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponseMessage<UserLoginDto>>> RefreshToken()
        {
            // Debugging aid (optional)
            // Console.WriteLine($"[Refresh] Cookie present: {Request.Cookies.ContainsKey("refreshToken")}");

            var refreshToken = Request.Cookies["refreshToken"];
            var result = await _userAuthentication.RefreshTokenAsync(refreshToken);

            if (result.IsSuccess && result.Data != null)
            {
                // Rotate the cookie with same flags
                Response.Cookies.Append("refreshToken", result.Data.newRefreshToken, BuildRefreshCookieOptions());

                // Do not expose the new refresh token
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
            var result = await _userAuthentication.LogoutAsync(refreshToken);

            if (result.IsSuccess)
            {
                // Delete cookie with matching flags/path
                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
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
