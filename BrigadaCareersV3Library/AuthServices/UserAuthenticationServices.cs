using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;
using BrigadaCareersV3Library.Entities;
using JobPostingLibrary.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        private const string RefreshLoginProvider = "userIdentity";
        private const string RefreshTokenName = "refresh_token";

        public UserAuthenticationService(
            UserManager<ApplicationIdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            BrigadaCareersDbv3Context appContext,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext identityDb,
            PreProdHrmsParallelContext dbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _appContext = appContext;
            _httpContextAccessor = httpContextAccessor;
            _identityDb = identityDb;
            _dbContext = dbContext;
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
                    var newRefreshToken = await GenerateRefreshToken(loginUser); // rotates in DB

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

        // === Helpers ===

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
                var currentUserId = await GetCurrentUserIdAsync();

                // Get user and details first
                var user = await _appContext.AspNetUsers
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

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
                    .FirstOrDefaultAsync(d => d.UserId.ToString() == currentUserId);

                if (userDetails == null)
                {
                    return new ApiResponseMessage<UserDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "User details not found"
                    };
                }

                var appBinary = userDetails.UserProfileImageId.HasValue
                    ? await _appContext.TblAppbinaries
                        .FirstOrDefaultAsync(a => a.Id == userDetails.UserProfileImageId.Value)
                    : null;

                //var getgends = await _dbContext.Hr201genders.ToListAsync();

                var gender = userDetails.Hr201GenderId.HasValue
                    ? await _dbContext.Hr201genders
                        .FirstOrDefaultAsync(g => g.Id == userDetails.Hr201GenderId.Value)
                    : null;

                var civilStatus = userDetails.Hr201CivilStatus.HasValue
                    ? await _dbContext.Hr201civilStatuses
                        .FirstOrDefaultAsync(cs => cs.Id == userDetails.Hr201CivilStatus.Value)
                    : null;

                var getDetails = new UserDto
                {
                    Id = Guid.Parse(user.Id),
                    FirstName = userDetails.FirstName,
                    LastName = userDetails.LastName,
                    Email = user.Email,
                    MiddleName = userDetails.MiddleName,
                    ContactNo = userDetails.ContactNo,
                    UserProfileByte = appBinary?.Byte,
                    Hr201GenderId = userDetails.Hr201GenderId,
                    Hr201CivilStatusId = userDetails.Hr201CivilStatus,
                    Gender = gender?.Name,
                    CivilStatus = civilStatus?.Name,
                    DateOfBirth = userDetails.DateOfBirth,
                    Address = userDetails.Address,
                    StreetDetails = userDetails.StreetDetails,
                    AboutMe = userDetails.AboutMe,
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

        public async Task<string> GetCurrentUserIdAsync()
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

            return userId; // keep as string
        }


        public async Task<ApiResponseMessage<string>> InsertOrUpdateUserProfile(InsertOrUpdateUserProfileDto input)
        {
            var response = new ApiResponseMessage<string>();

            try
            {
                //var currentUserId = "1b1e846a-fe12-42f3-8448-1ac60cbbc0a7";
                var currentUserId = await GetCurrentUserIdAsync();
                var userGuid = Guid.Parse(currentUserId);

                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == userGuid);

                if (userDetails == null)
                {
                    response.Data = null;
                    response.IsSuccess = false;
                    response.ErrorMessage = "User not found";
                    return response;
                }

                var hasNewImage = !string.IsNullOrEmpty(input.ProfileImageBase64);

                // 1) Remove only
                if (input.RemoveProfileImage && !hasNewImage)
                {
                    if (userDetails.UserProfileImageId.HasValue)
                    {
                        await SoftDeleteBinaryAsync(userDetails.UserProfileImageId.Value);
                        userDetails.UserProfileImageId = null;
                    }

                    await _appContext.SaveChangesAsync();

                    response.Data = "Removed";
                    response.IsSuccess = true;
                    return response;
                }

                // 2) Replace / 3) Insert or Update
                if (hasNewImage)
                {
                    if (input.RemoveProfileImage && userDetails.UserProfileImageId.HasValue)
                    {
                        await SoftDeleteBinaryAsync(userDetails.UserProfileImageId.Value);
                        userDetails.UserProfileImageId = null;
                    }

                    if (userDetails.UserProfileImageId == null)
                    {
                        var newId = await UploadNewProfileImageAsync(
                            input.ProfileImageBase64,
                            input.ProfileImageFileName,
                            input.ProfileImageContentType);

                        userDetails.UserProfileImageId = newId;
                        response.Data = "Inserted";
                    }
                    else
                    {
                        await UpdateProfileImageAsync(
                            userDetails.UserProfileImageId.Value,
                            input.ProfileImageBase64,
                            input.ProfileImageFileName,
                            input.ProfileImageContentType);

                        response.Data = input.RemoveProfileImage ? "Replaced" : "Updated";
                    }

                    await _appContext.SaveChangesAsync();
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
        // CREATE: returns new Guid
        private async Task<Guid> UploadNewProfileImageAsync(string base64Data, string fileName, string contentType)
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
                    Description = "User Profile Image".Substring(0, Math.Min(500, "User Profile Image".Length)),
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

        // UPDATE: modifies existing record
        private async Task UpdateProfileImageAsync(Guid existingId, string base64Data, string fileName, string contentType)
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
                appBinary.Description = "User Profile Image".Substring(0, Math.Min(500, "User Profile Image".Length));
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
    }
}
