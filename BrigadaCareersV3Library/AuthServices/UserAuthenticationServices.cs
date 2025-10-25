using BrigadaCareersV3Library.Amazon;
using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.Enums;
using BrigadaCareersV3Library.Dto.UserDto;
using BrigadaCareersV3Library.Entities;
using JobPostingLibrary.Entities;
using JobPostingLibrary.HrmsDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace BrigadaCareersV3Library.AuthServices
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly UserManager<ApplicationIdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly BrigadaCareersDbv3Context _appContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _identityDb;
        private readonly PreProdHrmsParallelContext _dbContext;
        private readonly S3AmazonServices _s3Service;

        private const string RefreshLoginProvider = "userIdentity";
        private const string RefreshTokenName = "refresh_token";

        public UserAuthenticationService(
            UserManager<ApplicationIdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            BrigadaCareersDbv3Context appContext,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext identityDb,
            PreProdHrmsParallelContext dbContext,
            S3AmazonServices s3Service)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _appContext = appContext;
            _httpContextAccessor = httpContextAccessor;
            _identityDb = identityDb;
            _dbContext = dbContext;
            _s3Service = s3Service;
        }

        public async Task<string> RegisteredUser(UserDto register)
        {
            if (register.Id == Guid.Empty)
            {
                await CreateUser(register);
            }
            else
            {
                await UpdateUserDetails(register);
            }
            return "Success";
        }
        public async Task<string> CreateUser(UserDto register)
        {
            try
            {
                var isExistUser = await _userManager.FindByNameAsync(register.UserName!);
                if (isExistUser != null) return "User Already Exists";

                var user = new ApplicationIdentityUser
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = register.UserName,
                    Email = register.Email,
                };

                var result = await _userManager.CreateAsync(user, register.Password!);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    return "Error :" + errors;
                }

                var userDetails = new TblUserDetail
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.Parse(user.Id),
                    FirstName = register.UserName,
                    LastName = register.UserName,
                    EmailAddress = register.Email,
                    IsActive = true,
                    CreationTime = DateTime.UtcNow,
                };
                await _appContext.TblUserDetails.AddAsync(userDetails);
                await _appContext.SaveChangesAsync();

                if (!await _roleManager.RoleExistsAsync(UserRole.User))
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.User));

                if (await _roleManager.RoleExistsAsync(UserRole.User))
                    await _userManager.AddToRoleAsync(user, UserRole.User);

                return user.Id;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task UpdateUserDetails(UserDto register)
        {

            var getDetails = await _appContext.TblUserDetails.Where(x => x.UserId == register.Id).FirstOrDefaultAsync();
            if (getDetails is not null)
            {
                getDetails.FirstName = register.FirstName;
                getDetails.LastName = register.LastName;
                getDetails.MiddleName = register.MiddleName;
                getDetails.ContactNo = register.ContactNo;
                getDetails.DateOfBirth = register.DateOfBirth;
                getDetails.Hr201GenderId = register.Hr201GenderId;
                getDetails.Hr201CivilStatus = register.Hr201CivilStatusId;
                getDetails.Address = register.Address;
                getDetails.StreetDetails = register.StreetDetails;
                getDetails.AboutMe = register.AboutMe;
            }
            else 
            {
                throw new Exception("No User Details");
            }

            await _appContext.SaveChangesAsync();
        }
        public async Task<string> RegisteredAdmin(RegisterUserDto register)
        {
            try
            {
                var isExistUser = await _userManager.FindByNameAsync(register.UserName);
                if (isExistUser != null) return "User Already Exists";

                var user = new ApplicationIdentityUser
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = register.UserName,
                    Email = ""
                };

                var result = await _userManager.CreateAsync(user, register.Password);
                if (!result.Succeeded) return "Error : Cannot Create Admin --> Please Try Again.";

                if (!await _roleManager.RoleExistsAsync(UserRole.Admin))
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.Admin));
                if (!await _roleManager.RoleExistsAsync(UserRole.User))
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.User));

                if (await _roleManager.RoleExistsAsync(UserRole.Admin))
                    await _userManager.AddToRoleAsync(user, UserRole.Admin);

                return "Success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<ApiResponseMessage<UserLoginDto>> LoginAccount(RegisterUserDto login)
        {
            try
            {
                var usernameExist = await _userManager.FindByNameAsync(login.UserName);
                var emailExist = await _userManager.FindByEmailAsync(login.UserName);

                var loginUser = usernameExist ?? emailExist;
                if (loginUser != null && await _userManager.CheckPasswordAsync(loginUser, login.Password))
                {
                    var userRole = await _userManager.GetRolesAsync(loginUser);

                    var accessToken = await GenerateAccessToken(loginUser, userRole);
                    var newRefreshToken = await GenerateRefreshToken(loginUser); 

                    var userInfo = new UserLoginDto
                    {
                        userID = loginUser.Id,
                        UserToken = accessToken,
                        newRefreshToken = newRefreshToken,
                        UserName = loginUser.UserName!,
                        UserRole = userRole.ToList()
                    };

                    return new ApiResponseMessage<UserLoginDto> { Data = userInfo, IsSuccess = true, ErrorMessage = "" };
                }

                return new ApiResponseMessage<UserLoginDto>
                {
                    Data = null!,
                    IsSuccess = false,
                    ErrorMessage = "No User Found --> Try Again"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<UserLoginDto> { Data = null!, IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
        public async Task<ApiResponseMessage<UserLoginDto>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return new ApiResponseMessage<UserLoginDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "Refresh token is required"
                    };
                }

             
                var user = await FindUserByRefreshTokenAsync(refreshToken);
                if (user == null)
                {
                    return new ApiResponseMessage<UserLoginDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "Invalid refresh token"
                    };
                }

              
                var isValidRefreshToken = await ValidateRefreshTokenAsync(user, refreshToken);
                if (!isValidRefreshToken)
                {
                    return new ApiResponseMessage<UserLoginDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "Refresh token is expired or invalid"
                    };
                }

                var roles = await _userManager.GetRolesAsync(user);
                var newAccessToken = await GenerateAccessToken(user, roles);
                var newRefreshToken = await GenerateRefreshToken(user); // rotate

                var userInfo = new UserLoginDto
                {
                    userID = user.Id,
                    UserToken = newAccessToken,
                    newRefreshToken = newRefreshToken,
                    UserName = user.UserName!,
                    UserRole = roles.ToList()
                };

                return new ApiResponseMessage<UserLoginDto> { Data = userInfo, IsSuccess = true, ErrorMessage = "" };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<UserLoginDto> { Data = null, IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
        public async Task<ApiResponseMessage<bool>> InvalidateRefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return new ApiResponseMessage<bool> { Data = false, IsSuccess = false, ErrorMessage = "Refresh token is required" };

                var user = await FindUserByRefreshTokenAsync(refreshToken);
                if (user != null)
                {
                    await RemoveRefreshTokenAsync(user);
                    return new ApiResponseMessage<bool> { Data = true, IsSuccess = true, ErrorMessage = "" };
                }

                return new ApiResponseMessage<bool> { Data = false, IsSuccess = false, ErrorMessage = "Invalid refresh token" };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<bool> { Data = false, IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
        public async Task<ApiResponseMessage<bool>> LogoutAsync(string refreshToken)
        {
            try
            {
                var result = new ApiResponseMessage<bool> { Data = true, IsSuccess = true, ErrorMessage = "" };

                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var user = await FindUserByRefreshTokenAsync(refreshToken);
                    if (user != null)
                    {
                        await RemoveRefreshTokenAsync(user);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<bool> { Data = false, IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
        private async Task<string> GenerateAccessToken(ApplicationIdentityUser user, IList<string> userRoles)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecreteKey"]!));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };
            foreach (var r in userRoles) claims.Add(new Claim(ClaimTypes.Role, r));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddHours(8),
                claims: claims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private async Task<string> GenerateRefreshToken(ApplicationIdentityUser user)
        {
            // Remove current value (keyed by UserId + provider + name)
            try { await _userManager.RemoveAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenName); } catch { /* ignore */ }

            // Generate a new provider token
            var newToken = await _userManager.GenerateUserTokenAsync(user, RefreshLoginProvider, RefreshTokenName);
            if (string.IsNullOrWhiteSpace(newToken))
                throw new InvalidOperationException("Failed to generate refresh token via provider.");

            // Persist to AspNetUserTokens
            var setResult = await _userManager.SetAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenName, newToken);
            if (!setResult.Succeeded)
                throw new InvalidOperationException("Failed to store refresh token via Identity.");

            return newToken;
        }
        private async Task<ApplicationIdentityUser?> FindUserByRefreshTokenAsync(string refreshToken)
        {
            var row = await _identityDb.Set<IdentityUserToken<string>>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.LoginProvider == RefreshLoginProvider &&
                    t.Name == RefreshTokenName &&
                    t.Value == refreshToken);

            if (row == null) return null;
            return await _userManager.FindByIdAsync(row.UserId);
        }
        private async Task<bool> ValidateRefreshTokenAsync(ApplicationIdentityUser user, string refreshToken)
        {
            // Enforces DataProtectionTokenProvider lifespan (configure to 7 days in Program.cs)
            return await _userManager.VerifyUserTokenAsync(user, RefreshLoginProvider, RefreshTokenName, refreshToken);
        }
        private async Task RemoveRefreshTokenAsync(ApplicationIdentityUser user)
        {
            try
            {
                await _userManager.RemoveAuthenticationTokenAsync(user, RefreshLoginProvider, RefreshTokenName);
            }
            catch
            {
                // ignore cleanup failures
            }
        }
        public async Task<ApiResponseMessage<UserDto>> getUserProfileDetails()
        {
            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                // Get user and details first
                var user = await _appContext.AspNetUsers
                    .FirstOrDefaultAsync(u => u.Id == currentUser.UserId.ToString());

                if (user == null)
                {
                    return new ApiResponseMessage<UserDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "User not found"
                    };
                }

                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(d => d.UserId.ToString() == currentUser.UserId.ToString());

                if (userDetails == null)
                {
                    return new ApiResponseMessage<UserDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "User details not found"
                    };
                }

                // 🔹 Fetch profile, cover, and resume files (if any)
                var appBinaryProfile = await _s3Service.GetUserFileByTypeAsync(userDetails.Id, FileTypeEnum.ProfileImage);
                var appBinaryCover = await _s3Service.GetUserFileByTypeAsync(userDetails.Id, FileTypeEnum.CoverImage);
                var appBinaryResume = await _s3Service.GetUserFileByTypeAsync(userDetails.Id, FileTypeEnum.Resume);

                // 🔹 Resolve S3 URLs
                var profileUrl = appBinaryProfile != null ? _s3Service.GetFileUrl(appBinaryProfile.S3key!) : null;
                var coverUrl = appBinaryCover != null ? _s3Service.GetFileUrl(appBinaryCover.S3key!) : null;
                var resumeUrl = appBinaryResume != null ? _s3Service.GetFileUrl(appBinaryResume.S3key!) : null;

                // 🔹 Optional: fetch lookup values for gender and civil status
                var gender = userDetails.Hr201GenderId.HasValue
                    ? await _dbContext.Hr201genders.FirstOrDefaultAsync(g => g.Id == userDetails.Hr201GenderId.Value)
                    : null;

                var civilStatus = userDetails.Hr201CivilStatus.HasValue
                    ? await _dbContext.Hr201civilStatuses.FirstOrDefaultAsync(cs => cs.Id == userDetails.Hr201CivilStatus.Value)
                    : null;

                // 🔹 Construct user DTO
                var getDetails = new UserDto
                {
                    Id = Guid.Parse(user.Id),
                    FirstName = userDetails.FirstName,
                    LastName = userDetails.LastName,
                    MiddleName = userDetails.MiddleName,
                    ContactNo = userDetails.ContactNo,
                    Email = user.Email,
                    Hr201GenderId = userDetails.Hr201GenderId,
                    Hr201CivilStatusId = userDetails.Hr201CivilStatus,
                    Gender = gender?.Name,
                    CivilStatus = civilStatus?.Name,
                    DateOfBirth = userDetails.DateOfBirth,
                    Address = userDetails.Address,
                    StreetDetails = userDetails.StreetDetails,
                    AboutMe = userDetails.AboutMe,

                    // ✅ Attach URLs here
                    UserProfileImage = profileUrl,
                    UserCoverPhotoImage = coverUrl,
                    UserResumeFile = resumeUrl
                };

                return new ApiResponseMessage<UserDto>
                {
                    Data = getDetails,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<UserDto>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GetCurrentUserIdAsyncDto> GetCurrentUserIdAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No HttpContext. User is not authenticated.");
            var user = httpContext.User ?? throw new InvalidOperationException("No User principal on HttpContext.");

            string[] idClaimTypes =
            {
        ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub, "sub", "oid", "uid", "userid", "user_id", "id", "nameid"
    };

            var userId = idClaimTypes
                .Select(t => user.FindFirstValue(t))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            if (string.IsNullOrWhiteSpace(userId))
            {
                var available = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));
                throw new InvalidOperationException("User ID claim is missing from the authentication token. Available claims: " + available);
            }

            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
                throw new Exception("User not found in identity system");

            // Parse BEFORE the query - this is the key fix
            var userGuid = Guid.Parse(userId);

            // Now use the parsed Guid in the query
            var joinUserDetails = await
                (
                from userdetails in _appContext.TblUserDetails
                where userdetails.UserId == userGuid 
                select new GetCurrentUserIdAsyncDto
                {
                    Id = userdetails.Id,
                    UserId = userdetails.UserId.Value,
                    FirstName = userdetails.FirstName!,
                    LastName = userdetails.LastName!,
                }
                ).FirstOrDefaultAsync();

            if (joinUserDetails == null)
                throw new Exception("User details not found");

            return joinUserDetails;
        }
        //public async Task<ApiResponseMessage<string>> InsertOrUpdateUserCoverPhoto(InsertOrUpdateUserCoverPhotoDto input)
        //{
        //    var response = new ApiResponseMessage<string>();

        //    try
        //    {
        //        //var currentUserId = "1b1e846a-fe12-42f3-8448-1ac60cbbc0a7";
        //        var currentUser = await GetCurrentUserIdAsync();


        //        var userDetails = await _appContext.TblUserDetails
        //            .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

        //        if (userDetails == null)
        //        {
        //            response.Data = null;
        //            response.IsSuccess = false;
        //            response.ErrorMessage = "User not found";
        //            return response;
        //        }

        //        var hasNewImage = !string.IsNullOrEmpty(input.CoverImageBase64);

        //        // 1) Remove only
        //        if (input.RemoveCoverImage && !hasNewImage)
        //        {
        //            if (userDetails.CoverPhotoImageId.HasValue)
        //            {
        //                await SoftDeleteBinaryAsync(userDetails.CoverPhotoImageId.Value);
        //                userDetails.CoverPhotoImageId = null;
        //            }

        //            await _appContext.SaveChangesAsync();

        //            response.Data = "Removed";
        //            response.IsSuccess = true;
        //            return response;
        //        }

        //        // 2) Replace / 3) Insert or Update
        //        if (hasNewImage)
        //        {
        //            if (input.RemoveCoverImage && userDetails.CoverPhotoImageId.HasValue)
        //            {
        //                await SoftDeleteBinaryAsync(userDetails.CoverPhotoImageId.Value);
        //                userDetails.UserProfileImageId = null;
        //            }

        //            if (userDetails.CoverPhotoImageId == null)
        //            {
        //                var newId = await UploadNewProfileImageAsync(
        //                    input.CoverImageBase64,
        //                    input.CoverImageFileName,
        //                    input.CoverImageContentType,
        //                    "User Profile Cover " + currentUser.FirstName
        //                    );

        //                userDetails.CoverPhotoImageId = newId;
        //                response.Data = "Inserted";
        //            }
        //            else
        //            {
        //                await UpdateProfileImageAsync(
        //                    userDetails.CoverPhotoImageId.Value,
        //                    input.CoverImageBase64,
        //                    input.CoverImageFileName,
        //                    input.CoverImageContentType,
        //                    "User Cover Image " + currentUser.FirstName

        //                    );

        //                response.Data = input.RemoveCoverImage ? "Replaced" : "Updated";
        //            }

        //            await _appContext.SaveChangesAsync();
        //            response.IsSuccess = true;
        //            return response;
        //        }


        //        response.Data = "No changes";
        //        response.IsSuccess = true;
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Data = null;
        //        response.IsSuccess = false;
        //        response.ErrorMessage = ex.Message;
        //        return response;
        //    }
        //}
        public async Task<ApiResponseMessage<string>> InsertOrUpdateUserCoverPhoto(InsertOrUpdateUserCoverPhotoDto input)
        {
            var response = new ApiResponseMessage<string>();
            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

                if (userDetails == null)
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.ErrorMessage = "User not found";
                    return response;
                }

                var hasNewImage = !string.IsNullOrEmpty(input.CoverImageBase64);

                // Get existing cover image
                var existingCoverImage = await _s3Service.GetUserFileByTypeAsync(
                    userDetails.UserId.Value,
                    FileTypeEnum.CoverImage
                );

                // 1) Remove only
                if (input.RemoveCoverImage && !hasNewImage)
                {
                    if (existingCoverImage != null)
                    {
                        await _s3Service.DeleteFileAsync(existingCoverImage.Id, userDetails.UserId.Value);
                    }

                    response.Data = "Removed";
                    response.IsSuccess = true;
                    return response;
                }

                // 2) Replace / 3) Insert or Update
                if (hasNewImage)
                {
                    bool isNewImage = existingCoverImage == null;

                    // Delete old image if exists (for both replace and update)
                    if (existingCoverImage != null)
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(existingCoverImage.Id, userDetails.UserId.Value);
                        }
                        catch
                        {
                            // Log error but continue with upload
                        }
                    }

                    // Upload new cover image
                    await _s3Service.UploadFileAsync(
                        base64Data: input.CoverImageBase64,
                        fileName: input.CoverImageFileName ?? "cover-image.jpg",
                        contentType: input.CoverImageContentType ?? "image/jpeg",
                        fileType: FileTypeEnum.CoverImage,
                        userId: userDetails.Id,
                        description: $"Cover Photo for {userDetails.FirstName} {userDetails.LastName}"
                    );

                    response.Data = isNewImage ? "Inserted" : (input.RemoveCoverImage ? "Replaced" : "Updated");
                    response.IsSuccess = true;
                    return response;
                }

                response.Data = "No changes";
                response.IsSuccess = true;
                return response;
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }
        //public async Task<ApiResponseMessage<string>> InsertOrUpdateUserProfile(InsertOrUpdateUserProfileDto input)
        //{
        //    var response = new ApiResponseMessage<string>();

        //    try
        //    {
        //        //var currentUserId = "1b1e846a-fe12-42f3-8448-1ac60cbbc0a7";
        //        var currentUser = await GetCurrentUserIdAsync();


        //        var userDetails = await _appContext.TblUserDetails
        //            .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

        //        if (userDetails == null)
        //        {
        //            response.Data = null;
        //            response.IsSuccess = false;
        //            response.ErrorMessage = "User not found";
        //            return response;
        //        }

        //        var hasNewImage = !string.IsNullOrEmpty(input.ProfileImageBase64);

        //        // 1) Remove only
        //        if (input.RemoveProfileImage && !hasNewImage)
        //        {
        //            if (userDetails.UserProfileImageId.HasValue)
        //            {
        //                await SoftDeleteBinaryAsync(userDetails.UserProfileImageId.Value);
        //                userDetails.UserProfileImageId = null;
        //            }

        //            await _appContext.SaveChangesAsync();

        //            response.Data = "Removed";
        //            response.IsSuccess = true;
        //            return response;
        //        }

        //        // 2) Replace / 3) Insert or Update
        //        if (hasNewImage)
        //        {
        //            if (input.RemoveProfileImage && userDetails.UserProfileImageId.HasValue)
        //            {
        //                await SoftDeleteBinaryAsync(userDetails.UserProfileImageId.Value);
        //                userDetails.UserProfileImageId = null;
        //            }

        //            if (userDetails.UserProfileImageId == null)
        //            {
        //                var newId = await UploadNewProfileImageAsync(
        //                    input.ProfileImageBase64,
        //                    input.ProfileImageFileName,
        //                    input.ProfileImageContentType,
        //                    "User Profile Image " + currentUser.FirstName
        //                    );

        //                userDetails.UserProfileImageId = newId;
        //                response.Data = "Inserted";
        //            }
        //            else
        //            {
        //                await UpdateProfileImageAsync(
        //                    userDetails.UserProfileImageId.Value,
        //                    input.ProfileImageBase64,
        //                    input.ProfileImageFileName,
        //                    input.ProfileImageContentType,
        //                    "User Profile Image " + currentUser.FirstName

        //                    );

        //                response.Data = input.RemoveProfileImage ? "Replaced" : "Updated";
        //            }

        //            await _appContext.SaveChangesAsync();
        //            response.IsSuccess = true;
        //            return response;
        //        }


        //        response.Data = "No changes";
        //        response.IsSuccess = true;
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Data = null;
        //        response.IsSuccess = false;
        //        response.ErrorMessage = ex.Message;
        //        return response;
        //    }
        //}
        public async Task<ApiResponseMessage<string>> InsertOrUpdateUserProfile(InsertOrUpdateUserProfileDto input)
        {
            var response = new ApiResponseMessage<string>();
            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

                if (userDetails == null)
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.ErrorMessage = "User not found";
                    return response;
                }

                var hasNewImage = !string.IsNullOrEmpty(input.ProfileImageBase64);

                // Get existing profile image
                var existingProfileImage = await _s3Service.GetUserFileByTypeAsync(
                    userDetails.UserId.Value,
                    FileTypeEnum.ProfileImage
                );

                // 1) Remove only
                if (input.RemoveProfileImage && !hasNewImage)
                {
                    if (existingProfileImage != null)
                    {
                        await _s3Service.DeleteFileAsync(existingProfileImage.Id, userDetails.UserId.Value);
                    }

                    response.Data = "Removed";
                    response.IsSuccess = true;
                    return response;
                }

                // 2) Replace / 3) Insert or Update
                if (hasNewImage)
                {
                    bool isNewImage = existingProfileImage == null;

                    // Delete old image if exists (for both replace and update)
                    if (existingProfileImage != null)
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(existingProfileImage.Id, userDetails.UserId.Value);
                        }
                        catch
                        {
                            // Log error but continue with upload
                        }
                    }

                    // Upload new image
                    await _s3Service.UploadFileAsync(
                        base64Data: input.ProfileImageBase64,
                        fileName: input.ProfileImageFileName ?? "profile-image.jpg",
                        contentType: input.ProfileImageContentType ?? "image/jpeg",
                        fileType: FileTypeEnum.ProfileImage,
                        userId: userDetails.Id,
                        description: $"Profile Image for {userDetails.FirstName} {userDetails.LastName}"
                    );

                    response.Data = isNewImage ? "Inserted" : (input.RemoveProfileImage ? "Replaced" : "Updated");
                    response.IsSuccess = true;
                    return response;
                }

                response.Data = "No changes";
                response.IsSuccess = true;
                return response;
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }
        //public async Task<ApiResponseMessage<string>> InsertOrUpdateUserResume(InsertOrUpdateUserResumeDto input)
        //{
        //    var response = new ApiResponseMessage<string>();

        //    try
        //    {
        //        //var currentUserId = "1b1e846a-fe12-42f3-8448-1ac60cbbc0a7";
        //        var currentUser = await GetCurrentUserIdAsync();


        //        var userDetails = await _appContext.TblUserDetails
        //            .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

        //        if (userDetails == null)
        //        {
        //            response.Data = null;
        //            response.IsSuccess = false;
        //            response.ErrorMessage = "User not found";
        //            return response;
        //        }

        //        var hasNewImage = !string.IsNullOrEmpty(input.UserResumeBase64);

        //        // 1) Remove only
        //        if (input.RemoveUserResume && !hasNewImage)
        //        {
        //            if (userDetails.ResumeId.HasValue)
        //            {
        //                await SoftDeleteBinaryAsync(userDetails.ResumeId!.Value);
        //                userDetails.ResumeId = null;
        //            }

        //            await _appContext.SaveChangesAsync();

        //            response.Data = "Removed";
        //            response.IsSuccess = true;
        //            return response;
        //        }

        //        // 2) Replace / 3) Insert or Update
        //        if (hasNewImage)
        //        {
        //            if (input.RemoveUserResume && userDetails.ResumeId.HasValue)
        //            {
        //                await SoftDeleteBinaryAsync(userDetails.ResumeId.Value);
        //                userDetails.ResumeId = null;
        //            }

        //            if (userDetails.ResumeId == null)
        //            {
        //                var newId = await UploadNewProfileImageAsync(
        //                    input.UserResumeBase64,
        //                    input.UserResumeFileName,
        //                    input.UserResumeContentType,
        //                    "User Resume " + currentUser.FirstName
        //                    );

        //                userDetails.ResumeId = newId;
        //                response.Data = "Inserted";
        //            }
        //            else
        //            {
        //                await UpdateProfileImageAsync(
        //                    userDetails.ResumeId.Value,
        //                    input.UserResumeBase64,
        //                    input.UserResumeFileName,
        //                    input.UserResumeContentType,
        //                    "User Resume "+ currentUser.FirstName

        //                    );

        //                response.Data = input.RemoveUserResume ? "Replaced" : "Updated";
        //            }

        //            await _appContext.SaveChangesAsync();
        //            response.IsSuccess = true;
        //            return response;
        //        }


        //        response.Data = "No changes";
        //        response.IsSuccess = true;
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Data = null;
        //        response.IsSuccess = false;
        //        response.ErrorMessage = ex.Message;
        //        return response;
        //    }
        //}
        public async Task<ApiResponseMessage<string>> InsertOrUpdateUserResume(InsertOrUpdateUserResumeDto input)
        {
            var response = new ApiResponseMessage<string>();

            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

                if (userDetails == null)
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.ErrorMessage = "User not found";
                    return response;
                }

                var hasNewResume = !string.IsNullOrEmpty(input.UserResumeBase64);

                // Get existing resume from appbinary table
                var existingResume = await _s3Service.GetUserFileByTypeAsync(
                    userDetails.UserId.Value,
                    FileTypeEnum.Resume
                );

                // 1) Remove only
                if (input.RemoveUserResume && !hasNewResume)
                {
                    if (existingResume != null)
                    {
                        await _s3Service.DeleteFileAsync(existingResume.Id, userDetails.UserId.Value);
                    }

                    response.Data = "Removed";
                    response.IsSuccess = true;
                    return response;
                }

                // 2) Replace or 3) Insert / Update
                if (hasNewResume)
                {
                    bool isNewResume = existingResume == null;

                    // Delete old file (replace/update case)
                    if (existingResume != null)
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(existingResume.Id, userDetails.UserId.Value);
                        }
                        catch
                        {
                            // swallow deletion errors to avoid blocking upload
                        }
                    }

                    // Upload new resume
                    await _s3Service.UploadFileAsync(
                        base64Data: input.UserResumeBase64,
                        fileName: input.UserResumeFileName ?? "resume.pdf",
                        contentType: input.UserResumeContentType ?? "application/pdf",
                        fileType: FileTypeEnum.Resume,
                        userId: userDetails.Id,
                        description: $"Resume file for {userDetails.FirstName} {userDetails.LastName}"
                    );

                    response.Data = isNewResume ? "Inserted" : (input.RemoveUserResume ? "Replaced" : "Updated");
                    response.IsSuccess = true;
                    return response;
                }

                // No new data, no deletion
                response.Data = "No changes";
                response.IsSuccess = true;
                return response;
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private async Task<Guid> UploadNewProfileImageAsync(string base64Data, string fileName, string contentType, string description)
        {
            try
            {
                var base64Content = base64Data.Contains(",") ? base64Data.Split(',')[1] : base64Data;
                var fileBytes = Convert.FromBase64String(base64Content);
                var fileExtension = GetFileExtension(contentType, fileName);

                var binaryId = Guid.NewGuid();
                var safeFileName = (fileName ?? $"profile_image_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}");
                var appBinary = new TblAppbinary
                {
                    Id = binaryId,
                    FileName = safeFileName.Substring(0, Math.Min(255, safeFileName.Length)),
                    Byte = fileBytes,
                    DateUpload = DateTime.UtcNow,
                    IsDeleted = false,
                    Description = description.Substring(0, Math.Min(500, description.Length)),
                    CreationTime = DateTime.UtcNow
                };

                _appContext.TblAppbinaries.Add(appBinary);
                await _appContext.SaveChangesAsync();

                return binaryId;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                throw new Exception($"Failed to upload profile image: {ex.Message}. Inner: {inner}");
            }
        }
        private async Task UpdateProfileImageAsync(Guid existingId, string base64Data, string fileName, string contentType, string description)
        {
            try
            {
                var base64Content = base64Data.Contains(",") ? base64Data.Split(',')[1] : base64Data;
                var fileBytes = Convert.FromBase64String(base64Content);
                var fileExtension = GetFileExtension(contentType, fileName);

                var appBinary = await _appContext.TblAppbinaries.FirstOrDefaultAsync(b => b.Id == existingId);
                if (appBinary == null)
                    throw new Exception("Existing profile image record not found.");

                var safeFileName = (fileName ?? $"profile_image_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}");
                appBinary.FileName = safeFileName.Substring(0, Math.Min(255, safeFileName.Length));
                appBinary.Byte = fileBytes;
                appBinary.DateUpload = DateTime.UtcNow;
                appBinary.IsDeleted = false;
                appBinary.Description = description.Substring(0, Math.Min(500, description.Length));
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                throw new Exception($"Failed to update profile image: {ex.Message}. Inner: {inner}");
            }
        }
        private string GetFileExtension(string contentType, string fileName)
        {
            // Try to get extension from content type first
            if (!string.IsNullOrEmpty(contentType))
            {
                return contentType.ToLower() switch
                {
                    "image/jpeg" or "image/jpg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    "image/bmp" => ".bmp",
                    "application/pdf" => ".pdf",
                    _ => ""
                };
            }

            // Fallback to filename extension
            if (!string.IsNullOrEmpty(fileName) && fileName.Contains('.'))
            {
                return Path.GetExtension(fileName).ToLower();
            }

            // Default to .jpg if nothing else works
            return ".jpg";
        }
        private async Task SoftDeleteBinaryAsync(Guid id)
        {
            var appBinary = await _appContext.TblAppbinaries.FirstOrDefaultAsync(b => b.Id == id);
            if (appBinary != null)
            {
                appBinary.IsDeleted = true; // Soft delete flag your model already uses
                                            // Optionally: appBinary.DateUpload = DateTime.UtcNow; // if you track change time here
            }
        }
        //EDUCATION
        public async Task<ApiResponseMessage<string>> CreateOrEditEducation(CreateOrEditEducationDto input)
        {
            if (input.Id == Guid.Empty)
            {
                //create
                try
                {
                    var currentUser = await GetCurrentUserIdAsync();
             


                    var insertEdu = new TblEducation
                    {
                        Id = Guid.NewGuid(),
                        UserIdFk = currentUser.Id,
                        SchoolName = input.SchoolName,
                        EducationLevel = input.EducationLevel,
                        Course = input.Course,
                        StartDate = input.StartDate,
                        EndDate = input.EndDate,
                        CreationTime = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _appContext.TblEducations.AddAsync(insertEdu);
                    await _appContext.SaveChangesAsync();

                    return new ApiResponseMessage<string>
                    {
                        Data = "Success",
                        IsSuccess = true,
                        ErrorMessage = ""
                    };
                }
                catch (Exception ex)
                {

                    return new ApiResponseMessage<string>
                    {
                        Data = "",
                        IsSuccess = false,
                        ErrorMessage = ex.Message   
                    };
                }


            }
            else 
            {
                //update
            }
            return null;
        }
        public async Task<ApiResponseMessage<IList<CreateOrEditEducationDto>>> GetUserEducation()
        {
            var currentUser = await GetCurrentUserIdAsync();

            var getUserEducation = await _appContext.TblEducations.Where(x => x.UserIdFk == currentUser.Id)
                .Select(x => new CreateOrEditEducationDto
                { 
                Id = x.Id,
                SchoolName = x.SchoolName,
                EducationLevel = x.EducationLevel,
                Course = x.Course,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                })
                .ToListAsync();

            return new ApiResponseMessage<IList<CreateOrEditEducationDto>>
            {
                Data = getUserEducation,
                IsSuccess = true,
                ErrorMessage = "",
            };
        }
        public async Task<ApiResponseMessage<string>> DeleteUserEducation(Guid educationId)
        {
            try
            {
                var apiMessage = "";
                var getUserEducation = await _appContext.TblEducations.Where(x => x.Id == educationId).FirstOrDefaultAsync();

                if (getUserEducation is null)
                {
                    apiMessage = "";
                }
                else 
                {
                    _appContext.TblEducations.Remove(getUserEducation!);
                    await _appContext.SaveChangesAsync();

                    apiMessage = "Success";
                }



                return new ApiResponseMessage<string>
                {
                    Data = apiMessage,
                    IsSuccess = !string.IsNullOrWhiteSpace(apiMessage),
                    ErrorMessage = !string.IsNullOrWhiteSpace(apiMessage) ? "" : "No Data"
                };
            }
            catch (Exception ex)
            {

                return new ApiResponseMessage<string>
                {
                    Data = "",
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                };
            }

        }
        //WORK EXPERIENCE
        public async Task<ApiResponseMessage<string>> CreateOrEditWorkExperience(CreateOrEditWorkExperienceDto input)
        {
            if (input.Id == Guid.Empty)
            {
                //create
                try
                {
                    var currentUser = await GetCurrentUserIdAsync();
                    var insertWorkExp = new TblWorkExperience
                    {
                        Id = Guid.NewGuid(),
                        UserIdFk = currentUser.Id,
                        CreationTime = DateTime.UtcNow,
                        IsDeleted = false,
                        CompanyAddress = input.CompanyAddress,
                        CompanyName = input.CompanyName,
                        JobTitle = input.JobTitle,
                        JobDescription = input.JobDescription,
                        StartDate = input.StartDate,
                        EndDate = input.EndDate,

                    };

                    await _appContext.TblWorkExperiences.AddAsync(insertWorkExp);
                    await _appContext.SaveChangesAsync();

                    return new ApiResponseMessage<string>
                    {
                        Data = "Success",
                        IsSuccess = true,
                        ErrorMessage = ""
                    };
                }
                catch (Exception ex)
                {

                    return new ApiResponseMessage<string>
                    {
                        Data = "",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                }


            }
            else
            {
                //update
            }
            return null;
        }
        public async Task<ApiResponseMessage<IList<CreateOrEditWorkExperienceDto>>> GetUserWorkExperience()
        {
            var currentUser = await GetCurrentUserIdAsync();

            var getUserWorkExp = await _appContext.TblWorkExperiences.Where(x => x.UserIdFk == currentUser.Id)
                .Select(x => new CreateOrEditWorkExperienceDto
                {
                    Id = x.Id,
                    CompanyName = x.CompanyName,
                    CompanyAddress = x.CompanyAddress,
                    JobDescription = x.JobDescription,
                    JobTitle = x.JobTitle,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                })
                .ToListAsync();

            return new ApiResponseMessage<IList<CreateOrEditWorkExperienceDto>>
            {
                Data = getUserWorkExp,
                IsSuccess = true,
                ErrorMessage = "",
            };
        }
        public async Task<ApiResponseMessage<string>> DeleteUserWorkExperience(Guid workexperienceId)
        {
            try
            {
                var apiMessage = "";
                var getUserWorkExp = await _appContext.TblWorkExperiences.Where(x => x.Id == workexperienceId).FirstOrDefaultAsync();

                if (getUserWorkExp is null)
                {
                    apiMessage = "";
                }
                else
                {
                    _appContext.TblWorkExperiences.Remove(getUserWorkExp!);
                    await _appContext.SaveChangesAsync();

                    apiMessage = "Success";
                }



                return new ApiResponseMessage<string>
                {
                    Data = apiMessage,
                    IsSuccess = !string.IsNullOrWhiteSpace(apiMessage),
                    ErrorMessage = !string.IsNullOrWhiteSpace(apiMessage) ? "" : "No Data"
                };
            }
            catch (Exception ex)
            {

                return new ApiResponseMessage<string>
                {
                    Data = "",
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                };
            }

        }

        //CERTIFICATE

        public async Task<ApiResponseMessage<string>> CreateOrEditCertificate(CreateOrEditCertificateDto input)
        {
            var response = new ApiResponseMessage<string>();

            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                // 🔹 Get user details
                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

                if (userDetails == null)
                {
                    response.IsSuccess = false;
                    response.ErrorMessage = "User not found";
                    return response;
                }

                bool hasNewImage = !string.IsNullOrEmpty(input.CertificateImageBase64);
                TblCertificate certEntity;

                // -----------------------------
                // CREATE NEW CERTIFICATE
                // -----------------------------
                if (input.Id == Guid.Empty)
                {
                    Guid? uploadedFileId = null;

                    if (hasNewImage)
                    {
                        uploadedFileId = await _s3Service.UploadFileAsync(
                            base64Data: input.CertificateImageBase64,
                            fileName: input.CertificateImageFileName ?? "certificate.jpg",
                            contentType: input.CertificateImageContentType ?? "image/jpeg",
                            fileType: FileTypeEnum.Certificate,
                            userId: userDetails.Id,
                            description: $"Certificate: {input.Name}"
                        );
                    }

                    certEntity = new TblCertificate
                    {
                        Id = Guid.NewGuid(),
                        UserIdFk = userDetails.Id,
                        CreationTime = DateTime.UtcNow,
                        IsDeleted = false,
                        Name = input.Name,
                        Issuer = input.Issuer,
                        Highlights = input.Highlights,
                        DateAchieved = input.DateAchieved,
                        CertificateType = input.CertificateType,
                        AppBinaryId = uploadedFileId // ✅ save uploaded file ID
                    };

                    await _appContext.TblCertificates.AddAsync(certEntity);
                    await _appContext.SaveChangesAsync();

                    response.Data = "Certificate created successfully";
                    response.IsSuccess = true;
                    return response;
                }

                // -----------------------------
                // UPDATE EXISTING CERTIFICATE
                // -----------------------------
                certEntity = await _appContext.TblCertificates
                    .FirstOrDefaultAsync(c => c.Id == input.Id && c.UserIdFk == userDetails.Id && !c.IsDeleted);

                if (certEntity == null)
                {
                    response.IsSuccess = false;
                    response.ErrorMessage = "Certificate not found";
                    return response;
                }

                // Update fields
                certEntity.Name = input.Name;
                certEntity.Issuer = input.Issuer;
                certEntity.Highlights = input.Highlights;
                certEntity.DateAchieved = input.DateAchieved;
                certEntity.CertificateType = input.CertificateType;

                // Handle image replacement
                if (hasNewImage)
                {
                    // Delete old file if it exists
                    if (certEntity.AppBinaryId.HasValue)
                    {
                        try
                        {
                            await _s3Service.DeleteFileAsync(certEntity.AppBinaryId.Value, userDetails.UserId.Value);
                        }
                        catch
                        {
                            // Ignore delete failure
                        }
                    }

                    // Upload new certificate image
                    var newFileId = await _s3Service.UploadFileAsync(
                        base64Data: input.CertificateImageBase64,
                        fileName: input.CertificateImageFileName ?? "certificate.jpg",
                        contentType: input.CertificateImageContentType ?? "image/jpeg",
                        fileType: FileTypeEnum.Certificate,
                        userId: userDetails.Id,
                        description: $"Updated certificate: {input.Name}"
                    );

                    certEntity.AppBinaryId = newFileId; 
                }

                _appContext.TblCertificates.Update(certEntity);
                await _appContext.SaveChangesAsync();

                response.Data = "Certificate updated successfully";
                response.IsSuccess = true;
                return response;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }




        //public async Task<ApiResponseMessage<string>> CreateOrEditCertificate(CreateOrEditCertificateDto input)
        //{
        //    if (input.Id == Guid.Empty)
        //    {
        //        //create
        //        try
        //        {
        //            var currentUser = await GetCurrentUserIdAsync();
        //            var insertCert = new TblCertificate
        //            {
        //                Id = Guid.NewGuid(),
        //                UserIdFk = currentUser.Id,
        //                CreationTime = DateTime.UtcNow,
        //                IsDeleted = false,
        //                Name = input.Name,
        //                Issuer = input.Issuer,
        //                Highlights = input.Highlights,
        //                DateAchieved = input.DateAchieved,
        //                CertificateType = input.CertificateType,
        //                AttachImgId = await UploadNewProfileImageAsync(
        //                    input.ProfileImageBase64,
        //                    input.ProfileImageFileName,
        //                    input.ProfileImageContentType,
        //                    "User Upload Certificate"
        //                    )

        //        };

        //            await _appContext.TblCertificates.AddAsync(insertCert);
        //            await _appContext.SaveChangesAsync();

        //            return new ApiResponseMessage<string>
        //            {
        //                Data = "Success",
        //                IsSuccess = true,
        //                ErrorMessage = ""
        //            };
        //        }
        //        catch (Exception ex)
        //        {

        //            return new ApiResponseMessage<string>
        //            {
        //                Data = "",
        //                IsSuccess = false,
        //                ErrorMessage = ex.Message
        //            };
        //        }


        //    }
        //    else
        //    {
        //        //update
        //    }
        //    return null;
        //}
        public async Task<ApiResponseMessage<IList<GetUserCertificateDto>>> GetUserCertificate()
        {
            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                // Get all user certificates
                var certificates = await _appContext.TblCertificates
                    .Where(cert => cert.UserIdFk == currentUser.Id && !cert.IsDeleted)
                    .OrderByDescending(cert => cert.DateAchieved)
                    .ToListAsync();

                var result = new List<GetUserCertificateDto>();

                if (certificates.Any())
                {
                
                    var appbinaryIds = certificates
                        .Where(c => c.AppBinaryId != null)
                        .Select(c => c.AppBinaryId.Value)
                        .ToList();

               
                    var certificateImages = await _appContext.TblAppbinaries
                        .Where(ab => appbinaryIds.Contains(ab.Id)
                                  && ab.TypeEnum == (int)FileTypeEnum.Certificate
                                  && !ab.IsDeleted)
                        .ToListAsync();

                    foreach (var cert in certificates)
                    {
                        var image = certificateImages.FirstOrDefault(ab => ab.Id == cert.AppBinaryId);

                        result.Add(new GetUserCertificateDto
                        {
                            Id = cert.Id,
                            Name = cert.Name,
                            Issuer = cert.Issuer,
                            Highlights = cert.Highlights,
                            DateAchieved = cert.DateAchieved,
                            Type = (CertificateTypeEnum)cert.CertificateType,
                            ImageUrl = image != null ? _s3Service.GetFileUrl(image.S3key) : null,
                            FileName = image?.FileName,
                            AppBinaryId = image.Id,
                        });
                    }
                }

                return new ApiResponseMessage<IList<GetUserCertificateDto>>
                {
                    Data = result,
                    IsSuccess = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<IList<GetUserCertificateDto>>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }



        public async Task<ApiResponseMessage<string>> DeleteUserCertificate(Guid certificateId, Guid appBinaryID)
        {
            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                var certificate = await _appContext.TblCertificates
                    .FirstOrDefaultAsync(x => x.Id == certificateId && x.UserIdFk == currentUser.Id);

                if (certificate == null)
                {
                    return new ApiResponseMessage<string>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "Certificate not found"
                    };
                }

                // Get all related certificate images
                var certificateImages = await _appContext.TblAppbinaries
                    .Where(ab => ab.UserId == currentUser.Id
                              && ab.TypeEnum == (int)FileTypeEnum.Certificate
                              && ab.Id == appBinaryID
                              && !ab.IsDeleted)
                    .ToListAsync();

                foreach (var image in certificateImages)
                {
                    try
                    {
                        // Mark as deleted
                        image.IsDeleted = true;

                        // Delete from S3 (if exists)
                        if (!string.IsNullOrEmpty(image.S3key))
                        {
                            await _s3Service.DeleteFileAsync(image.Id, currentUser.Id);

                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but continue
                        // _logger.LogWarning($"Failed to delete S3 file: {ex.Message}");
                    }
                }

                // Soft delete certificate
                certificate.IsDeleted = true;
                _appContext.TblCertificates.Update(certificate);

                // Save changes for both images and certificate
                await _appContext.SaveChangesAsync();

                return new ApiResponseMessage<string>
                {
                    Data = "Certificate deleted successfully",
                    IsSuccess = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<string>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        //SKILLS
        public async Task<ApiResponseMessage<string>> CreateOrEditSkills(CreateOrEditSkillsDto input)
        {
            if (input.Id == Guid.Empty)
            {
                //create
                try
                {
                    var currentUser = await GetCurrentUserIdAsync();
                    var insertSkill = new TblSkill
                    {
                        Id = Guid.NewGuid(),
                        UserIdFk = currentUser.Id,
                        CreationTime = DateTime.UtcNow,
                        IsDeleted = false,
                        Name = input.Name,

                    };

                    await _appContext.TblSkills.AddAsync(insertSkill);
                    await _appContext.SaveChangesAsync();

                    return new ApiResponseMessage<string>
                    {
                        Data = "Success",
                        IsSuccess = true,
                        ErrorMessage = ""
                    };
                }
                catch (Exception ex)
                {

                    return new ApiResponseMessage<string>
                    {
                        Data = "",
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                }


            }
            else
            {
                //update
            }
            return null;
        }
        public async Task<ApiResponseMessage<IList<CreateOrEditSkillsDto>>> GetUserSkills()
        {
            var currentUser = await GetCurrentUserIdAsync();
            var getUserSkills = await _appContext.TblSkills.Where(x => x.UserIdFk == currentUser.Id)
                .Select(x => new CreateOrEditSkillsDto
                {
                    Id = x.Id,
                    Name = x.Name,
              
                })
                .ToListAsync();


            return new ApiResponseMessage<IList<CreateOrEditSkillsDto>>
            {
                Data = getUserSkills,
                IsSuccess = true,
                ErrorMessage = "",
            };
        }
        public async Task<ApiResponseMessage<string>> DeleteUserSkills(Guid skillId)
        {
            try
            {
                var apiMessage = "";
                var getUserSkill = await _appContext.TblSkills.Where(x => x.Id == skillId).FirstOrDefaultAsync();

                if (getUserSkill is null)
                {
                    apiMessage = "";
                }
                else
                {
                    _appContext.TblSkills.Remove(getUserSkill!);
                    await _appContext.SaveChangesAsync();

                    apiMessage = "Success";
                }



                return new ApiResponseMessage<string>
                {
                    Data = apiMessage,
                    IsSuccess = !string.IsNullOrWhiteSpace(apiMessage),
                    ErrorMessage = !string.IsNullOrWhiteSpace(apiMessage) ? "" : "No Data"
                };
            }
            catch (Exception ex)
            {

                return new ApiResponseMessage<string>
                {
                    Data = "",
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                };
            }

        }

        //public async Task<ApiResponseMessage<string>> CreateOrUpdateReqSubmission(CreateOrUpdateReqSubmissionDto input)
        //{
        //    var response = new ApiResponseMessage<string>();

        //    try
        //    {
        //        var currentUser = await GetCurrentUserIdAsync();

        //        var userDetails = await _appContext.TblUserDetails
        //            .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

        //        if (userDetails == null)
        //        {
        //            response.Data = null;
        //            response.IsSuccess = false;
        //            response.ErrorMessage = "User not found";
        //            return response;
        //        }

        //        // Find existing requirement for this user + checklist item combination
        //        var getReq = await _appContext.TblUserRequirements
        //            .Where(x => x.UserDetailsId == userDetails.Id
        //                     && x.RecrtmntRequirementChecklistId == input.RecrtmntRequirementCheckListId
        //                     && !x.IsDeleted)
        //            .FirstOrDefaultAsync();

        //        var hasNewImage = !string.IsNullOrEmpty(input.UserReqFileBase64);

        //        // 1) Remove only
        //        if (input.RemoveUserReqFile && !hasNewImage)
        //        {
        //            if (getReq != null)
        //            {
        //                await SoftDeleteBinaryAsync(getReq.Id);
        //                getReq.IsDeleted = true;
        //                await _appContext.SaveChangesAsync();

        //                response.Data = "Removed";
        //                response.IsSuccess = true;
        //                return response;
        //            }

        //            response.Data = "No requirement found to remove";
        //            response.IsSuccess = true;
        //            return response;
        //        }

        //        // 2) Replace / 3) Insert or Update
        //        if (hasNewImage)
        //        {
        //            // If replacing existing requirement
        //            if (input.RemoveUserReqFile && getReq != null)
        //            {
        //                await SoftDeleteBinaryAsync(getReq.Id);
        //                getReq.IsDeleted = true;
        //            }

        //            // INSERT: No existing requirement found
        //            if (getReq == null)
        //            {
        //                var newBinaryId = await UploadNewProfileImageAsync(
        //                    input.UserReqFileBase64,
        //                    input.UserReqFileName,
        //                    input.UserReqFileContentType,
        //                    "User File Requirements " + currentUser.FirstName
        //                );

        //                var insertUserReq = new TblUserRequirement
        //                {
        //                    Id = Guid.NewGuid(),
        //                    UseReqId = newBinaryId,
        //                    UserDetailsId = userDetails.Id,
        //                    RecrtmntRequirementChecklistId = input.RecrtmntRequirementCheckListId,
        //                    Status = (int)RequirementsStatusEnum.Pending,
        //                    CreationTime = DateTime.UtcNow,
        //                    IsDeleted = false,
        //                };

        //                await _appContext.TblUserRequirements.AddAsync(insertUserReq);
        //                await _appContext.SaveChangesAsync();

        //                response.Data = "Inserted";
        //                response.IsSuccess = true;
        //                return response;
        //            }
        //            // UPDATE: Requirement exists
        //            else
        //            {
        //                await UpdateProfileImageAsync(
        //                    getReq.UseReqId.Value,
        //                    input.UserReqFileBase64,
        //                    input.UserReqFileName,
        //                    input.UserReqFileContentType,
        //                    "User File Requirements " + currentUser.FirstName
        //                );

        //                getReq.Status = (int)RequirementsStatusEnum.Pending; // Reset to pending on update
        //                await _appContext.SaveChangesAsync();

        //                response.Data = input.RemoveUserReqFile ? "Replaced" : "Updated";
        //                response.IsSuccess = true;
        //                return response;
        //            }
        //        }

        //        response.Data = "No changes";
        //        response.IsSuccess = true;
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Data = null;
        //        response.IsSuccess = false;
        //        response.ErrorMessage = ex.Message;
        //        return response;
        //    }
        //}

        public async Task<ApiResponseMessage<string>> CreateOrUpdateReqSubmission(CreateOrUpdateReqSubmissionDto input)
        {
            var response = new ApiResponseMessage<string>();

            try
            {
                var currentUser = await GetCurrentUserIdAsync();

                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

                if (userDetails == null)
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.ErrorMessage = "User not found";
                    return response;
                }

                // Find existing requirement for this user + checklist item combination
                var getReq = await _appContext.TblUserRequirements
                    .Where(x => x.UserDetailsId == userDetails.Id
                             && x.RecrtmntRequirementChecklistId == input.RecrtmntRequirementCheckListId
                             && !x.IsDeleted)
                    .FirstOrDefaultAsync();

                var hasNewImage = !string.IsNullOrEmpty(input.UserReqFileBase64);

                // ✅ 1) Remove only
                if (input.RemoveUserReqFile && !hasNewImage)
                {
                    if (getReq != null)
                    {
                        // Soft delete existing binary + record
                        if (getReq.UseReqId.HasValue)
                        {
                            await SoftDeleteBinaryAsync(getReq.UseReqId.Value);
                        }
                        getReq.IsDeleted = true;
                        await _appContext.SaveChangesAsync();

                        response.Data = "Removed";
                        response.IsSuccess = true;
                        return response;
                    }

                    response.Data = "No requirement found to remove";
                    response.IsSuccess = true;
                    return response;
                }

                // ✅ 2) Replace / 3) Insert or Update
                if (hasNewImage)
                {
                    // If replacing existing requirement, delete old binary
                    if (input.RemoveUserReqFile && getReq != null && getReq.UseReqId.HasValue)
                    {
                        await SoftDeleteBinaryAsync(getReq.UseReqId.Value);
                    }

                    // Prepare upload to S3
                    var fileName = input.UserReqFileName ?? "requirement-file.jpg";
                    var contentType = input.UserReqFileContentType ?? "image/jpeg";

                    // Upload to S3 and get the TblAppbinary ID
                    var uploadedFileId = await _s3Service.UploadFileAsync(
                        base64Data: input.UserReqFileBase64,
                        fileName: fileName,
                        contentType: contentType,
                        fileType: FileTypeEnum.Attachment,
                        userId: userDetails.Id,
                        description: $"Requirement: {fileName}"
                    );

                    // INSERT: No existing requirement found
                    if (getReq == null)
                    {
                        var insertUserReq = new TblUserRequirement
                        {
                            Id = Guid.NewGuid(),
                            UseReqId = uploadedFileId, 
                            UserDetailsId = userDetails.Id,
                            RecrtmntRequirementChecklistId = input.RecrtmntRequirementCheckListId,
                            Status = (int)RequirementsStatusEnum.Pending,
                            CreationTime = DateTime.UtcNow,
                            IsDeleted = false,
                        };

                        await _appContext.TblUserRequirements.AddAsync(insertUserReq);
                        await _appContext.SaveChangesAsync();

                        response.Data = "Inserted";
                        response.IsSuccess = true;
                        return response;
                    }
                    else
                    {
                        // UPDATE: Existing record, update with new file reference
                        getReq.UseReqId = uploadedFileId;  // ✅ Store the new TblAppbinary ID
                        getReq.Status = (int)RequirementsStatusEnum.Pending; // reset to pending on update
                        await _appContext.SaveChangesAsync();

                        response.Data = input.RemoveUserReqFile ? "Replaced" : "Updated";
                        response.IsSuccess = true;
                        return response;
                    }
                }

                // No changes made
                response.Data = "No changes";
                response.IsSuccess = true;
                return response;
            }
            catch (Exception ex)
            {
                response.Data = null;
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }
        public async Task<ApiResponseMessage<IList<GetRequirmentsDto>>> GetRequirementsV1()
        {
            try
            {

                var currentUser = await GetCurrentUserIdAsync();

                // First, get data from HRMS context
                var hrmsReqs = await _dbContext.RecrtmntRequirementChecklists
                    .Select(hrmsreq => new
                    {
                        hrmsreq.Id,
                        hrmsreq.Name
                    })
                    .ToListAsync();

                // Get the IDs to filter the second query
                var hrmsReqIds = hrmsReqs.Select(h => h.Id).ToList();

                // Second, get data from App context with binary information
                var userReqsWithBinaries = await (
                    from userreq in _appContext.TblUserRequirements
                    where hrmsReqIds.Contains(userreq.RecrtmntRequirementChecklistId)
                          && !userreq.IsDeleted
                          && userreq.UserDetailsId == currentUser.Id  
                    join appbinary in _appContext.TblAppbinaries
                        on userreq.UseReqId equals appbinary.Id into appbinaryGroup
                    from appbinary in appbinaryGroup.DefaultIfEmpty()
                    select new
                    {
                        userreq.RecrtmntRequirementChecklistId,
                        FileName = appbinary != null ? appbinary.FileName : null,
                        DateUpload = appbinary != null ? appbinary.CreationTime : (DateTime?)null,
                        userreq.Remarks,
                        userreq.Status,
                        S3Key = appbinary != null ? appbinary.S3key : null
                    }
                ).ToListAsync();

                // Join in memory and build final result
                var getReq = hrmsReqs
                    .GroupJoin(
                        userReqsWithBinaries,
                        h => h.Id,
                        u => u.RecrtmntRequirementChecklistId,
                        (h, userGroup) => new { h, userGroup })
                    .SelectMany(
                        x => x.userGroup.DefaultIfEmpty(),
                        (x, u) => new GetRequirmentsDto
                        {
                            Id = x.h.Id,
                            CheckListName = x.h.Name,
                            FileName = u?.FileName ?? "-",
                            DateUpload = u?.DateUpload?.ToString("MMM dd, yyyy") ?? "-",
                            Remarks = u?.Remarks ?? "-",
                            Status = u != null ? ((RequirementsStatusEnum)u.Status).ToString() : string.Empty,
                            ImageUrl = u?.S3Key != null ? _s3Service.GetFileUrl(u.S3Key) : null 
                        })
                    .ToList();

                return new ApiResponseMessage<IList<GetRequirmentsDto>>
                {
                    Data = getReq,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<IList<GetRequirmentsDto>>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message  
                };
            }
        }
        public async Task<ApiResponseMessage<string>> ValidationPrimarySecondary(Guid userId)
        {
            try
            {
                var educationRecords = await _appContext.TblEducations
                    .Where(x => x.UserIdFk == userId &&
                           (x.EducationLevel == "Primary education" || x.EducationLevel == "Secondary education"))
                    .ToListAsync();

                var hasPrimary = educationRecords.Any(x => x.EducationLevel == "Primary education");
                var hasSecondary = educationRecords.Any(x => x.EducationLevel == "Secondary education");

                if (hasPrimary && hasSecondary)
                {
                    return new ApiResponseMessage<string>
                    {
                        Data = "Both Primary and Secondary Education are already provided.",
                        IsSuccess = false,
                        ErrorMessage = string.Empty
                    };
                }

                var missingEducation = (!hasPrimary, !hasSecondary) switch
                {
                    (true, true) => "Please Input Primary or Secondary Education before proceeding",
                    (true, false) => "Please Input Primary Education before proceeding",
                    (false, true) => "Please Input Secondary Education before proceeding",
                    _ => string.Empty
                };

                return new ApiResponseMessage<string>
                {
                    Data = missingEducation,
                    IsSuccess = !string.IsNullOrEmpty(missingEducation),
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<string>
                {
                    Data = string.Empty,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // Main Application Method
        public async Task<ApiResponseMessage<string>> InsertToApplicantMasterList([FromBody] applicantdataDto applyDto)
        {
            try
            {
                var currentUser = await GetCurrentUserIdAsync();
                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

                if (userDetails == null)
                {
                    return new ApiResponseMessage<string>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "User details not found"
                    };
                }

                var validationResult = await ValidationPrimarySecondary(currentUser.Id);
                if (validationResult.IsSuccess)
                {
                    return new ApiResponseMessage<string>
                    {
                        Data = "NoPrimaryOrSecondary",
                        IsSuccess = false,
                        ErrorMessage = validationResult.Data
                    };
                }

                if (!IsUserProfileComplete(userDetails))
                {
                    return new ApiResponseMessage<string>
                    {
                        Data = "Missing Data",
                        IsSuccess = false,
                        ErrorMessage = "Please input missing profile data before proceeding!"
                    };
                }

                var existingMasterlist = await _dbContext.RecrtmntApplicantMasterlists
                    .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId);

                if (existingMasterlist != null)
                {
                    return await HandleExistingApplicant(existingMasterlist, userDetails, applyDto, currentUser.UserId);
                }

                return await CreateNewApplicant(userDetails, applyDto, currentUser.UserId);
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<string>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        // Helper Methods
        private bool IsUserProfileComplete(TblUserDetail userDetails)
        {
            var requiredFields = new[]
            {
                userDetails.FirstName,
                userDetails.LastName,
                userDetails.ContactNo,
                userDetails.EmailAddress,
                userDetails.DateOfBirth?.ToString(),
                userDetails.Hr201GenderId?.ToString(),
                userDetails.Hr201CivilStatus?.ToString(),
                userDetails.Address
    };

            return !requiredFields.Any(string.IsNullOrEmpty);
        }

        private async Task<ApiResponseMessage<string>> HandleExistingApplicant(
            RecrtmntApplicantMasterlist masterlist,
            TblUserDetail userDetails,
            applicantdataDto applyDto,
            Guid userId)
        {
            UpdateMasterlistFromUserDetails(masterlist, userDetails);
            await _dbContext.SaveChangesAsync();

            var activeApplication = await _dbContext.RecrtmntJobPostingDetails
                .Where(x => x.RecrtmntApplicantMasterlistId == masterlist.Id &&
                       (x.Status == (int)ApplicationStatus.Hired || x.Status == (int)ApplicationStatus.OnProgress))
                .FirstOrDefaultAsync();

            if (activeApplication != null)
            {
                return activeApplication.Status == (int)ApplicationStatus.Hired
                    ? CreateErrorResponse("You're already hired; you can't apply for this job.")
                    : CreateErrorResponse("The user application is currently ongoing with another job.");
            }

            var jobPostingDetail = await _dbContext.RecrtmntJobPostingDetails
                .FirstOrDefaultAsync(x => x.RecrtmntApplicantMasterlistId == masterlist.Id &&
                                    x.RecrtmntJobPostingHeaderId == applyDto.jobPostingId);

            if (jobPostingDetail != null)
            {
                return HandleExistingJobApplication(jobPostingDetail);
            }

            return await CreateNewJobApplication(masterlist.Id, applyDto, userId);
        }

        private void UpdateMasterlistFromUserDetails(RecrtmntApplicantMasterlist masterlist, TblUserDetail userDetails)
        {
            masterlist.FirstName = userDetails.FirstName!;
            masterlist.MiddleName = userDetails.MiddleName ?? string.Empty;
            masterlist.LastName = userDetails.LastName!;
            masterlist.ContactNo = userDetails.ContactNo;
            masterlist.EmailAddress = userDetails.EmailAddress!;
            masterlist.DateOfBirth = userDetails.DateOfBirth;
            masterlist.Address = userDetails.Address!;
            masterlist.Hr201genderId = userDetails.Hr201GenderId;
            masterlist.Hr201civilStatusId = userDetails.Hr201CivilStatus;
            masterlist.Type = 1;
        }

        private ApiResponseMessage<string> HandleExistingJobApplication(RecrtmntJobPostingDetail jobPostingDetail)
        {
            var statusMessages = new Dictionary<ApplicationStatus, string>
    {
        { ApplicationStatus.Failed, "Oops! It seems your previous application didn't quite hit the mark. Feel free to explore other opportunities or reach out if you have questions about your previous application. Good luck out there!" },
        { ApplicationStatus.Cancelled, "Oops! It seems your previous application didn't quite hit the mark. Feel free to explore other opportunities or reach out if you have questions about your previous application. Good luck out there!" },
        { ApplicationStatus.OnProgress, "The user application is in progress!" },
        { ApplicationStatus.ForPooling, "The user application is for pooling!" },
        { ApplicationStatus.Pending, "The user application is still pending!" }
    };

            var status = (ApplicationStatus)jobPostingDetail.Status;

            if (statusMessages.TryGetValue(status, out var message))
            {
                return CreateErrorResponse(message);
            }

            return CreateErrorResponse("Unable to process application.");
        }

        private async Task<ApiResponseMessage<string>> CreateNewJobApplication(Guid masterlistId, applicantdataDto applyDto, Guid userId)
        {
            var allowedStatuses = new[]
            {
        (int)ApplicationStatus.Pending,
        (int)ApplicationStatus.OnProgress,
        (int)ApplicationStatus.Cancelled,
        (int)ApplicationStatus.Qualified,
        (int)ApplicationStatus.Rejected
    };

            var applicationCount = await _dbContext.RecrtmntJobPostingDetails
                .CountAsync(x => x.RecrtmntApplicantMasterlistId == masterlistId &&
                            allowedStatuses.Contains(x.Status));

            if (applicationCount >= 3)
            {
                return CreateErrorResponse("The user has exceeded the limit of 3 applications.");
            }

            var jobPostingDetail = await CreateJobPostingDetail(masterlistId, applyDto, userId);
            await _dbContext.RecrtmntJobPostingDetails.AddAsync(jobPostingDetail);
            await _dbContext.SaveChangesAsync();

            return new ApiResponseMessage<string>
            {
                Data = null,
                IsSuccess = true,
                ErrorMessage = string.Empty
            };
        }

        private async Task<ApiResponseMessage<string>> CreateNewApplicant(TblUserDetail userDetails, applicantdataDto applyDto, Guid userId)
        {
            var masterlist = new RecrtmntApplicantMasterlist
            {
                Id = Guid.NewGuid(),
                TenantId = 2,
                CreationTime = DateTime.Now,
                CreatorUserId = 2,
                FirstName = userDetails.FirstName!,
                MiddleName = userDetails.MiddleName ?? string.Empty,
                LastName = userDetails.LastName!,
                ContactNo = userDetails.ContactNo,
                EmailAddress = userDetails.EmailAddress!,
                DateOfBirth = userDetails.DateOfBirth,
                Address = userDetails.Address!,
                Hr201genderId = userDetails.Hr201GenderId,
                Hr201civilStatusId = userDetails.Hr201CivilStatus,
                UserId = userDetails.UserId,
                Type = 1,
                RecrtmntJobPostingDetails = new List<RecrtmntJobPostingDetail>
        {
            await CreateJobPostingDetail(Guid.NewGuid(), applyDto, userId)
        }
            };

            _dbContext.RecrtmntApplicantMasterlists.Add(masterlist);
            await _dbContext.SaveChangesAsync();

            return new ApiResponseMessage<string>
            {
                Data = null,
                IsSuccess = true,
                ErrorMessage = string.Empty
            };
        }

        private async Task<RecrtmntJobPostingDetail> CreateJobPostingDetail(Guid masterlistId, applicantdataDto applyDto, Guid userId)
        {
            var detailId = Guid.NewGuid();
            var applicantNo = await GenerateApplicantNo(applyDto);

            return new RecrtmntJobPostingDetail
            {
                Id = detailId,
                RecrtmntApplicantMasterlistId = masterlistId,
                RecrtmntJobPostingHeaderId = applyDto.jobPostingId,
                PlantillaJobTitleId = applyDto.jobTitleId,
                Status = (int)ApplicationStatus.Pending,
                TenantId = 2,
                ApplicantNo = applicantNo,
                Stage = 0,
                CreationTime = DateTime.Now,
                IsDeleted = false,
                RecrtmntJobPostingDetailAuditLogs = new List<RecrtmntJobPostingDetailAuditLog>
        {
            new RecrtmntJobPostingDetailAuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = 2,
                RecrtmntJobPostingDetailId = detailId,
                Description = $"Applied for the job on {DateTime.Now:d}",
                CreationTime = DateTime.Now,
                CreatorUserId = 2,
                IsDeleted = false
            }
        }
            };
        }

        private async Task<string> GenerateApplicantNo(applicantdataDto applyDto)
        {
            var applicantCount = await _dbContext.RecrtmntJobPostingDetails
                .CountAsync(x => x.RecrtmntJobPostingHeaderId == applyDto.jobPostingId);

            return $"AN-{applicantCount + 1:D4}";
        }

        private ApiResponseMessage<string> CreateErrorResponse(string errorMessage)
        {
            return new ApiResponseMessage<string>
            {
                Data = null,
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        public async Task<ApiResponseMessage<IList<ApplicantJobLogsHeaderDto>>> GetJobApplicationStatus()
        {
            try
            {

                var currentUser = await GetCurrentUserIdAsync();


                var applicantMasterlist = await _dbContext.RecrtmntApplicantMasterlists
                    .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId);

                if (applicantMasterlist is null)
                {
                    return new ApiResponseMessage<IList<ApplicantJobLogsHeaderDto>>
                    {
                        Data = new List<ApplicantJobLogsHeaderDto>(),
                        IsSuccess = true,
                        ErrorMessage = string.Empty
                    };
                }

                var jobLogsQuery = from applicant in _dbContext.RecrtmntApplicantMasterlists
                                   join postingDetail in _dbContext.RecrtmntJobPostingDetails
                                       on applicant.Id equals postingDetail.RecrtmntApplicantMasterlistId
                                   join auditLog in _dbContext.RecrtmntJobPostingDetailAuditLogs
                                       on postingDetail.Id equals auditLog.RecrtmntJobPostingDetailId
                                   join header in _dbContext.RecrtmntJobPostingHeaders
                                       on postingDetail.RecrtmntJobPostingHeaderId equals header.Id
                                   join mrDetails in _dbContext.Mrdetails
                                       on header.MrdetailId equals mrDetails.Id
                                   join jobManagement in _dbContext.PlantillaJobMngmnts
                                       on mrDetails.PlantillaJobMngmntId equals jobManagement.Id
                                   join busUnit in _dbContext.Hr201businessUnits
                                       on jobManagement.Hr201businessUnitId equals busUnit.Id
                                   join location in _dbContext.Hr201locations
                                       on jobManagement.Hr201locationId equals location.Id
                                   join jobTitle in _dbContext.PlantillaJobTitles
                                       on jobManagement.PlantillaJobTitleId equals jobTitle.Id
                                   where applicant.Id == applicantMasterlist.Id
                                       && auditLog.IsHiddenLog == false
                                   orderby auditLog.CreationTime ascending
                                   select new
                                   {
                                       JobName = jobTitle.Name,
                                       businessUnitName = busUnit.Name,
                                       Location = location.Name,
                                       DescriptionLogs = auditLog.Description,
                                       Status = postingDetail.Status,
                                       JobStatus = header.Status,
                                       CreationTime = auditLog.CreationTime,
                                       MrfCategoryString = mrDetails.Mrfcategory,
                                   };

                var jobLogsData = await jobLogsQuery.ToListAsync();

                var groupedLogs = jobLogsData
                    .GroupBy(x => new { x.JobName, x.Status, x.JobStatus, x.MrfCategoryString, x.Location, x.businessUnitName })
                    .Select(group => new ApplicantJobLogsHeaderDto
                    {
                        JobName = group.Key.JobName,
                        status = group.Key.Status,
                        jobstatus = group.Key.JobStatus,
                        MrfCategory = group.Key.MrfCategoryString != null
                        ? ((MRFCategory)ConvertToNewEnum(group.Key.MrfCategoryString)).ToString()
                        : string.Empty,
                        LocationName = group.Key.Location,
                        BusinessUnitName = group.Key.businessUnitName,
                        ApplicantJobLogsDtos = group.Select(item => new ApplicantJobLogsDto
                        {
                            JobNameMother = item.JobName,
                            DescriptionLogs = ExtractDescription(item.DescriptionLogs),
                            status = item.Status,
                            CreationTime = item.CreationTime
                        }).ToList()
                    })
                    .ToList();

                return new ApiResponseMessage<IList<ApplicantJobLogsHeaderDto>>
                {
                    Data = groupedLogs,
                    IsSuccess = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<IList<ApplicantJobLogsHeaderDto>>
                {
                    Data = new List<ApplicantJobLogsHeaderDto>(),
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        private static string ExtractDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            return description.Contains(";")
                ? description.Split(';')[0]
                : description;
        }

        private static int ConvertToNewEnum(int oldValue)
        {
            return oldValue switch
            {
                0 => 0, // Regular_FullTime -> FullTime
                1 => 1, // Regular_PartTime -> PartTime
                2 => 0, // Casual_Full_Time -> FullTime
                3 => 1, // Casual_Part_Time -> PartTime
                _ => oldValue // Keep other values as-is
            };
        }

        public async Task<ApiResponseMessage<IList<GetUserJobOfferDtoV1>>> GetUserJobOffer()
        {
            try
            {

                var currentUser = await GetCurrentUserIdAsync();

          
                var rawData = await (
                    from applicant in _dbContext.RecrtmntApplicantMasterlists
                    join jobPosting in _dbContext.RecrtmntJobPostingDetails
                        on applicant.Id equals jobPosting.RecrtmntApplicantMasterlistId
                    join contract in _dbContext.Contracts
                        on jobPosting.Id equals contract.RecrtmntJobPostDetailId
                    join header in _dbContext.RecrtmntJobPostingHeaders
                        on jobPosting.RecrtmntJobPostingHeaderId equals header.Id
                    join mrDetails in _dbContext.Mrdetails
                        on header.MrdetailId equals mrDetails.Id
                    join jobTitle in _dbContext.PlantillaJobTitles
                        on contract.PlantillaJobTitleId equals jobTitle.Id into jobTitleGroup
                    from jobTitle in jobTitleGroup.DefaultIfEmpty()
                    join jobManagement in _dbContext.PlantillaJobMngmnts
                        on jobTitle.Id equals jobManagement.PlantillaJobTitleId
                    join busUnit in _dbContext.Hr201businessUnits
                        on jobManagement.Hr201businessUnitId equals busUnit.Id
                    join location in _dbContext.Hr201locations
                        on jobManagement.Hr201locationId equals location.Id
                    join appbinary in _dbContext.AppBinaryObjects
                        on contract.JobOfferPdf equals appbinary.Id into appbinaryGroup
                    from appbinary in appbinaryGroup.DefaultIfEmpty()
                    where applicant.UserId == currentUser.UserId && !contract.IsDeleted
                    select new
                    {
                        contract.Id,
                        JobTitle = jobTitle!.Name,
                        contract.StartDate,
                        contract.NoLaterThan,
                        contract.IsRejected,
                        contract.IsConfirmedByPmd,
                        contract.RejectionRemarks,
                        PdfByte = appbinary != null ? appbinary.Bytes : null,
                        BusinessUnitName = busUnit.Name,
                        LocationName = location.Name,
                        MrfCategoryString = mrDetails.Mrfcategory
                    }
                ).AsNoTracking().ToListAsync();


                var result = rawData
                    .GroupBy(r => r.Id)
                    .Select(g =>
                    {
                        var x = g.First();
                        return new GetUserJobOfferDtoV1
                        {
                            JobTitle = x.JobTitle,
                            ContractId = x.Id,
                            StartDate = x.StartDate.ToString("MMM dd, yyyy"),
                            NoLaterThan = x.NoLaterThan.ToString("MMM dd, yyyy"),
                            isRejected = x.IsRejected,
                            isConfirmed = x.IsConfirmedByPmd,
                            RejectionRemarks = x.RejectionRemarks,
                            PdfByte = x.PdfByte,
                            BusinessUnitName = x.BusinessUnitName,
                            LocationName = x.LocationName,
                            MrfCategory = Enum.IsDefined(typeof(MRFCategory), x.MrfCategoryString)
                            ? ((MRFCategory)x.MrfCategoryString).ToString()
                            : string.Empty
                        };
                    })
                    .ToList();






                return new ApiResponseMessage<IList<GetUserJobOfferDtoV1>>
                {
                    Data = result,
                    IsSuccess = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<IList<GetUserJobOfferDtoV1>>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<ApiResponseMessage<string>> UpdateJobOfferStatus(UpdateJobOfferStatusDto Dto)
        {
            try
            {
                var apiMessage = "";
                var updateStats = await _dbContext.Contracts.FirstOrDefaultAsync(x => x.Id == Dto.ContractId);
                if (updateStats != null)
                {
                    if (Dto.IsRejected == false)
                    {
                        updateStats.IsRejected = false;
                        updateStats.RejectionRemarks = null;
                    }
                    else
                    {
                        updateStats.IsRejected = true;
                        updateStats.RejectionRemarks = Dto.RejectionRemarks;
                    }

                    _dbContext.Contracts.Update(updateStats);
                    await _dbContext.SaveChangesAsync();

                    apiMessage = "Saved";
                }
                else
                {
                    apiMessage = "No Data";
                }


                return new ApiResponseMessage<string>
                {
                    Data = apiMessage,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<string>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
    }
}
