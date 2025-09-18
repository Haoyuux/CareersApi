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
            return Ok(result);

        }
    }
}
