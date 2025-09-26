using JobPostingLibrary.HrmsDtos;

namespace JobPostingLibrary.HrmsServices
{
    public interface IHrmsService
    {
        Task<ApiResponseMessageHrms<IList<GetAllJobPostsDto>>> GetAllJobPosts();
        Task<ApiResponseMessageHrms<IList<GetAllGenderDto>>> GetAllGender();
        Task<ApiResponseMessageHrms<IList<GetAllCivilStatusDto>>> GetAllCivilStatus();
    }
}