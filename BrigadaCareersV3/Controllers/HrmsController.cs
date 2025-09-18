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


    }
}
