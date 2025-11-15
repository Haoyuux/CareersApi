using BrigadaCareersV3Library.ApiResponseMessage;
using MailKit.Security;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.Entities;
using BrigadaCareersV3Library.Dto.Enums;
using BrigadaCareersV3Library.Dto;
using Microsoft.Extensions.Options;

namespace BrigadaCareersV3Library.OtpServices
{
   


    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationIdentityUser> _userManager;
        private readonly BrigadaCareersDbv3Context _appContext;
        private readonly SmtpSettings _smtpSettings;

        public OtpService(IMemoryCache cache, UserManager<ApplicationIdentityUser> userManager, BrigadaCareersDbv3Context appContext, IOptions<SmtpSettings> smtpSettings)
        {
            _cache = cache;
            _userManager = userManager;
            _appContext = appContext;
            _smtpSettings = smtpSettings.Value;
        }


        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // ---------------------------------------
        // Save OTP to memory for 5 minutes
        // ---------------------------------------
        private void SaveOtp(string email, string otp)
        {
            _cache.Set($"OTP_{email}", otp, TimeSpan.FromMinutes(5));
        }

        // ---------------------------------------
        // Public method: Generate OTP + Send Email (with purpose)
        // ---------------------------------------
        public async Task<ApiResponseMessage<string>> GenerateAndSendOtpAsync(
            string email,
            string firstname,
            string username,
            OtpPurpose purpose)
        {
            var otp = GenerateOtp();

            SaveOtp(email, otp);

            var result = await SendOtpEmailAsync(email, firstname, otp, username, purpose);

            return result;
        }

        // ---------------------------------------
        // Overload for backward compatibility (defaults to Registration)
        // ---------------------------------------
        public async Task<ApiResponseMessage<string>> GenerateAndSendOtpAsync(string email, string firstname, string username)
        {
            return await GenerateAndSendOtpAsync(email, firstname, username, OtpPurpose.Registration);
        }

        // ---------------------------------------
        // Email Sending Method (MailKit) - Enhanced with Purpose
        // ---------------------------------------
        private async Task<ApiResponseMessage<string>> SendOtpEmailAsync(
            string email,
            string firstname,
            string otp,
            string username,
            OtpPurpose purpose)
        {
            try
            {
                // Validation based on purpose
                if (purpose == OtpPurpose.Registration)
                {
                    // Check if username already exists
                    var isExistUser = await _userManager.FindByNameAsync(username);
                    if (isExistUser != null)
                    {
                        return new ApiResponseMessage<string>
                        {
                            Data = "User Already Exists",
                            IsSuccess = false,
                            ErrorMessage = "Username is already taken"
                        };
                    }

                    // Check if email already exists
                    var emailExisting = await _appContext.TblUserDetails
                        .Where(x => x.EmailAddress == email)
                        .FirstOrDefaultAsync();

                    if (emailExisting != null)
                    {
                        return new ApiResponseMessage<string>
                        {
                            Data = "Your Email Already Exists",
                            IsSuccess = false,
                            ErrorMessage = "This email is already registered"
                        };
                    }
                }
                else if (purpose == OtpPurpose.PasswordReset)
                {
                    // Check if email exists in the system
                    var emailExists = await _appContext.TblUserDetails
                        .Where(x => x.EmailAddress == email)
                        .FirstOrDefaultAsync();

                    if (emailExists == null)
                    {
                        return new ApiResponseMessage<string>
                        {
                            Data = "Email Not Found",
                            IsSuccess = false,
                            ErrorMessage = "This email is not registered in our system"
                        };
                    }
                }

                // CONFIGURE YOUR SMTP HERE
                string smtpHost = _smtpSettings.Host;
                int smtpPort = _smtpSettings.Port;

                string smtpUsername = _smtpSettings.Username;
                string smtpPassword = _smtpSettings.Password;

                // Create email with dynamic content based on purpose
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Brigada Careers", smtpUsername));
                message.To.Add(new MailboxAddress("", email));

                string subject = purpose == OtpPurpose.Registration
                    ? "Email Verification"
                    : "Password Reset Verification";

                message.Subject = subject;

                string emailBody = purpose == OtpPurpose.Registration
                    ? $"Hello {firstname},\n\n" +
                      $"Thank you for registering with Brigada Careers.\n\n" +
                      $"Your verification code is: {otp}\n\n" +
                      $"This code will expire in 5 minutes.\n\n" +
                      $"Thanks,\nBrigada Careers Team"
                    : $"Hello {firstname},\n\n" +
                      $"You have requested to reset your password.\n\n" +
                      $"Your verification code is: {otp}\n\n" +
                      $"This code will expire in 5 minutes.\n\n" +
                      $"If you did not request this, please ignore this email.\n\n" +
                      $"Thanks,\nBrigada Careers Team";

                var body = new BodyBuilder()
                {
                    TextBody = emailBody
                };

                message.Body = body.ToMessageBody();

                // Send email using SMTP
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(new NetworkCredential(smtpUsername, smtpPassword));
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                string successMessage = purpose == OtpPurpose.Registration
                    ? "Verification email sent successfully"
                    : "Password reset code sent successfully";

                return new ApiResponseMessage<string>
                {
                    Data = successMessage,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<string>
                {
                    Data = "Failed",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ---------------------------------------
        // Verify OTP
        // ---------------------------------------
        public Task<ApiResponseMessage<bool>> VerifyOtp(string email, string otp)
        {
            if (_cache.TryGetValue($"OTP_{email}", out string savedOtp))
            {
                bool isMatch = savedOtp == otp;

                return Task.FromResult(new ApiResponseMessage<bool>
                {
                    Data = isMatch,
                    IsSuccess = isMatch,
                    ErrorMessage = isMatch ? "" : "Invalid OTP"
                });
            }

            return Task.FromResult(new ApiResponseMessage<bool>
            {
                Data = false,
                IsSuccess = false,
                ErrorMessage = "OTP not found or expired"
            });
        }


        // ---------------------------------------
        // Remove OTP after success
        // ---------------------------------------
        public void RemoveOtp(string email)
        {
            _cache.Remove($"OTP_{email}");
        }

    }

}