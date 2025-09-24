using BrigadaCareersV3Library.ApiResponseMessage;
using JobPostingLibrary.HrmsDtos;

namespace JobPostingLibrary.HrmsServices
{
    public interface IHrmsService
    {
        Task<ApiResponseMessage<IList<GetAllJobPostsDto>>> GetAllJobPosts();
        Task<ApiResponseMessage<IList<GetAllGenderDto>>> GetAllGender();
        Task<ApiResponseMessage<IList<GetAllCivilStatusDto>>> GetAllCivilStatus();
    }
}