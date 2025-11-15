using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Dto.Enums;

namespace BrigadaCareersV3Library.OtpServices
{
    public interface IOtpService
    {
   
        Task<ApiResponseMessage<string>> GenerateAndSendOtpAsync(
            string email,
            string firstname,
            string username,
            OtpPurpose purpose);

        Task<ApiResponseMessage<string>> GenerateAndSendOtpAsync(
            string email,
            string firstname,
            string username);

        Task<ApiResponseMessage<bool>> VerifyOtp(string email, string otp);

      
        void RemoveOtp(string email);
    }
}