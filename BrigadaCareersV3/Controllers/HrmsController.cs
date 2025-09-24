using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.AuthServices;
using JobPostingLibrary.HrmsDtos;
using JobPostingLibrary.HrmsServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrigadaCareersV3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HrmsController : ControllerBase
    {
        private readonly IHrmsService _hrmsService;
        public HrmsController(IHrmsService hrmsService)
        {
            _hrmsService = hrmsService;
        }

        [HttpGet("GetAllJobPosts")]
        public async Task<ActionResult<ApiResponseMessage<IList<GetAllJobPostsDto>>>> GetAllJobPosts()
        {
            var res = await _hrmsService.GetAllJobPosts();
            if (res is not null)
            {
                return Ok(res);
            }
            return BadRequest(res);
        }

        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {

            var res = new
            {
                IsAuthenticated = User?.Identity?.IsAuthenticated ?? false,
                Name = User?.Identity?.Name,
                Claims = User?.Claims.Select(c => new { c.Type, c.Value })
            };

            return Ok(res);


        }

        [HttpGet("GetAllGender")]
        public async Task<ActionResult<ApiResponseMessage<IList<GetAllGenderDto>>>> GetAllGender()
        {
            try
            {
                var res = await _hrmsService.GetAllGender();
                return res;
            }
            catch (Exception ex)
            {
                var res = new ApiResponseMessage<IList<GetAllGenderDto>>
                {
                    Data = [],
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException.Message
                };
                return res;
            }

        }

        [HttpGet("GetAllCivilStatus")]
        public async Task<ActionResult<ApiResponseMessage<IList<GetAllCivilStatusDto>>>> GetAllCivilStatus()
        {
            try
            {
                var res = await _hrmsService.GetAllCivilStatus();
                return res;
            }
            catch (Exception ex)
            {
                var res = new ApiResponseMessage<IList<GetAllCivilStatusDto>>
                {
                    Data = [],
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException.Message
                };
                return res;
            }

        }
    }
}
