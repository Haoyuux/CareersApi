using JobPostingLibrary.Entities;
using JobPostingLibrary.HrmsDtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPostingLibrary.HrmsServices
{
    public class HrmsService : IHrmsService
    {
        private readonly PreProdHrmsParallelContext _dbContext;
        //private readonly BrigadaCareersDbv3Context _context;
        public HrmsService(PreProdHrmsParallelContext dbContext)
        {
            _dbContext = dbContext;
       
        }


        public async Task<ApiResponseMessageHrms<IList<GetAllJobPostsDto>>> GetAllJobPosts()
        {
            try
            {


                var _result = (await (from postingheaders in _dbContext.RecrtmntJobPostingHeaders
                                      join mrdetail in _dbContext.Mrdetails on postingheaders.MrdetailId equals mrdetail.Id
                                      join jobads in _dbContext.RecrtmntJobAdvertisements on mrdetail.Id equals jobads.MrdetailId
                                      join binaryobj in _dbContext.AppBinaryObjects on jobads.BinaryObjectId equals binaryobj.Id
                                      join plantjobmngt in _dbContext.PlantillaJobMngmnts on mrdetail.PlantillaJobMngmntId equals plantjobmngt.Id
                                      join busUnit in _dbContext.Hr201businessUnits on plantjobmngt.Hr201businessUnitId equals busUnit.Id
                                      join location in _dbContext.Hr201locations on plantjobmngt.Hr201locationId equals location.Id
                                      join plantrnks in _dbContext.PlantillaRanks on plantjobmngt.PlantillaRankId equals plantrnks.Id
                                      join planjobtitle in _dbContext.PlantillaJobTitles on plantjobmngt.PlantillaJobTitleId equals planjobtitle.Id
                                      where jobads.SourcingOption == 2 && plantjobmngt.IsDeleted == false && postingheaders.Status == 0
                                      select new { busUnit, location, plantrnks, planjobtitle, plantjobmngt, postingheaders, jobads, binaryobj })
                                     .OrderByDescending(x => x.postingheaders.CreationTime)
                                     .ToListAsync())
                                     .Select(x => new GetAllJobPostsDto
                                     {
                                         BussinessUnit = x.busUnit.Name ?? "",
                                         JobPostingId = x.postingheaders.Id,
                                         JobAdsPic = x.binaryobj.Bytes ?? Array.Empty<byte>(),
                                         Location = x.location.Name ?? "",
                                         MinRate = x.location.DailyRatePerArea,
                                         MaxRate = x.plantrnks.MaxRate,
                                         JobTitleName = x.planjobtitle.Name ?? "",
                                         JobTitleId = x.planjobtitle.Id,
                                         JobDescription = x.plantjobmngt.JobDescription ?? "",
                                         EssentialDuties = x.plantjobmngt.EssentialDutiesResponsibility ?? "",
                                         Education = x.plantjobmngt.Education ?? "",
                                         Specialized = x.plantjobmngt.SpecializedKnowledge ?? "",
                                         Skills = x.plantjobmngt.Skills ?? "",
                                         Experience = x.plantjobmngt.Experienced ?? "",
                                         ProfessionalCert = x.plantjobmngt.ProfessionalCertification ?? "",
                                         SpecialSkills = x.plantjobmngt.Skills ?? "",
                                         Training = x.plantjobmngt.Training ?? "",
                                         WorkingConditions = x.plantjobmngt.WorkingConditions ?? "",
                                         CreationTime = x.postingheaders.CreationTime,
                                         IntroStatement = x.jobads.IntroStatement ?? "",
                                         LogoInitial = GetBusinessUnitInitials(x.busUnit.Name ?? "")
                                     }).ToList();

                if (_result is null)
                {
                    return new ApiResponseMessageHrms<IList<GetAllJobPostsDto>>
                    {
                        Data = _result!,
                        IsSuccess = false,
                        ErrorMessage = "NO DATA"
                    };
                 
                }


                return new ApiResponseMessageHrms<IList<GetAllJobPostsDto>>
                {
                    Data = _result,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
           
            }
            catch (Exception ex)
            {
                return new ApiResponseMessageHrms<IList<GetAllJobPostsDto>>
                {
                    Data = [],
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException!.Message

                };
          

            }
        }
        private string GetBusinessUnitInitials(string businessUnitName)
        {
            // Handle null, empty, or whitespace-only strings
            if (string.IsNullOrWhiteSpace(businessUnitName))
                return string.Empty;

            try
            {
                var words = businessUnitName.Split(new char[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
                var initials = string.Join("", words.Select(word => 
                    word.Length > 0 ? word[0].ToString().ToUpper() : string.Empty));

                return initials;
            }
            catch
            {
                // Return empty string if any error occurs during processing
                return string.Empty;
            }
        }
        public async Task<ApiResponseMessageHrms<IList<GetAllGenderDto>>> GetAllGender()
        {
            try
            {
                var _data = await _dbContext.Hr201genders.Select(x => new GetAllGenderDto
                {


                    Id = x.Id,
                    Name = x.Name,


                }).ToListAsync();

                var result = new ApiResponseMessageHrms<IList<GetAllGenderDto>>
                {
                    Data = _data,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
                return result;
            }
            catch (Exception ex)
            {
                var result = new ApiResponseMessageHrms<IList<GetAllGenderDto>>
                {
                    Data = [],
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException.Message

                };
                return result;

            }
        }
        public async Task<ApiResponseMessageHrms<IList<GetAllCivilStatusDto>>> GetAllCivilStatus()
        {
            try
            {
                var _data = await _dbContext.Hr201civilStatuses.Select(x => new GetAllCivilStatusDto
                {


                    Id = x.Id,
                    Name = x.Name,


                }).ToListAsync();




                var result = new ApiResponseMessageHrms<IList<GetAllCivilStatusDto>>
                {
                    Data = _data,
                    IsSuccess = true,
                    ErrorMessage = ""


                };
                return result;
            }
            catch (Exception ex)
            {
                var result = new ApiResponseMessageHrms<IList<GetAllCivilStatusDto>>
                {
                    Data = [],
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException.Message

                };
                return result;

            }
        }
    }
}
