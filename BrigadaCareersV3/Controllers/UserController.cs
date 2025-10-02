using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.AuthServices;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using static BrigadaCareersV3Library.AuthServices.UserAuthenticationService;
using static System.Net.WebRequestMethods;

namespace BrigadaCareersV3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserAuthenticationService _userAuthentication;
        private readonly HttpClient _http;
        private readonly IHttpClientFactory _httpFactory;

        public UserController(IUserAuthenticationService userAuthentication, HttpClient http, IHttpClientFactory httpFactory)
        {
            _userAuthentication = userAuthentication;
            _http = http;
            _httpFactory = httpFactory;
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
        public async Task<ActionResult<ApiResponseMessage<UserDto>>> getUserProfileDetails()
        {
            var result = await _userAuthentication.getUserProfileDetails();
            return Ok(result);
        }

        [HttpPost("InsertOrUpdateUserProfile")]
        public async Task<ActionResult<ApiResponseMessage<string>>> InsertOrUpdateUserProfile([FromBody] InsertOrUpdateUserProfileDto input)
        {
            try
            {
                var result = await _userAuthentication.InsertOrUpdateUserProfile(input);
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseMessage<string>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public record NominatimResult(
            [property: JsonPropertyName("place_id")] long PlaceId,
            [property: JsonPropertyName("display_name")] string DisplayName,
            [property: JsonPropertyName("lat")] string Lat,
            [property: JsonPropertyName("lon")] string Lon,
            [property: JsonPropertyName("address")] Dictionary<string, string>? Address
        );


        [HttpGet("search")]
        [AllowAnonymous]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<NominatimResult>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<NominatimResult>>> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("q is required.");

            var http = _httpFactory.CreateClient("nominatim");
            var url = $"search?format=jsonv2&addressdetails=1&limit=10&accept-language=en&q={Uri.EscapeDataString(q)}";

            var results = await http.GetFromJsonAsync<List<NominatimResult>>(url);
            return Ok(results ?? new List<NominatimResult>());
        }


        [Authorize]
        [HttpPost("CreateOrEditEducation")]
        public async Task<ActionResult<ApiResponseMessage<string>>> CreateOrEditEducation([FromBody] CreateOrEditEducationDto input)
        {
  

            var result = await _userAuthentication.CreateOrEditEducation(input);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("GetUserEducation")]
        public async Task<ActionResult<ApiResponseMessage<IList<CreateOrEditEducationDto>>>> GetUserEducation()
        {
            var result = await _userAuthentication.GetUserEducation();
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpDelete("DeleteUserEducation")]
        public async Task<ActionResult<ApiResponseMessage<string>>> DeleteUserEducation(Guid educationId)
        {
            var result = await _userAuthentication.DeleteUserEducation(educationId);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("CreateOrEditWorkExperience")]
        public async Task<ActionResult<ApiResponseMessage<string>>> CreateOrEditWorkExperience([FromBody] CreateOrEditWorkExperienceDto input)
        {


            var result = await _userAuthentication.CreateOrEditWorkExperience(input);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("GetUserWorkExperience")]
        public async Task<ActionResult<ApiResponseMessage<IList<CreateOrEditWorkExperienceDto>>>> GetUserWorkExperience()
        {
            var result = await _userAuthentication.GetUserWorkExperience();
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpDelete("DeleteUserWorkExperience")]
        public async Task<ActionResult<ApiResponseMessage<string>>> DeleteUserWorkExperience(Guid workexperienceId)
        {
            var result = await _userAuthentication.DeleteUserWorkExperience(workexperienceId);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPost("CreateOrEditCertificate")]
        public async Task<ActionResult<ApiResponseMessage<string>>> CreateOrEditCertificate([FromBody] CreateOrEditCertificateDto input)
        {


            var result = await _userAuthentication.CreateOrEditCertificate(input);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("GetUserCertificate")]
        public async Task<ActionResult<ApiResponseMessage<IList<GetUserCertificateDto>>>> GetUserCertificate()
        {
            var result = await _userAuthentication.GetUserCertificate();
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpDelete("DeleteUserCertificate")]
        public async Task<ActionResult<ApiResponseMessage<string>>> DeleteUserCertificate(Guid certificateId)
        {
            var result = await _userAuthentication.DeleteUserCertificate(certificateId);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

    }
}
