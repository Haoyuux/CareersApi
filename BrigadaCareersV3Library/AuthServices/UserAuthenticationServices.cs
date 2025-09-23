using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;
using BrigadaCareersV3Library.Entities;
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

        // 🔑 Inject the Identity DB so we can query AspNetUserTokens directly
        private readonly ApplicationDbContext _identityDb;

        private const string RefreshLoginProvider = "userIdentity";
        private const string RefreshTokenName = "refresh_token";

        public UserAuthenticationService(
            UserManager<ApplicationIdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            BrigadaCareersDbv3Context appContext,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext identityDb)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _appContext = appContext;
            _httpContextAccessor = httpContextAccessor;
            _identityDb = identityDb;
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

        public async Task<string> UpdateUserDetails(UserDto register)
        {
            // implement as needed
            return null!;
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

        // 🔄 Refresh using cookie
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

                // ✅ O(1) lookup in AspNetUserTokens
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

                // ✅ Verify token against the provider (enforces lifespan)
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

        // ✅ Generate & persist the refresh token via Identity's token store
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

        public async Task<ApiResponseMessage<getUserProfileDetailsDto>> getUserProfileDetails()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var userDetails = await _appContext.TblUserDetails
                    .FirstOrDefaultAsync(u => u.UserId == Guid.Parse(currentUserId));

                if (userDetails == null)
                {
                    return new ApiResponseMessage<getUserProfileDetailsDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "User profile details not found"
                    };
                }

                var getDetails = new getUserProfileDetailsDto
                {
                    FirstName = userDetails.FirstName,
                    LastName = userDetails.LastName,
                    
                };
                

              
                return new ApiResponseMessage<getUserProfileDetailsDto> 
                { 
                   Data = getDetails, 
                   IsSuccess = true, 
                   ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<getUserProfileDetailsDto> { Data = null, IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public string GetCurrentUserId()
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

            var identityUser = _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                throw new Exception("User not found in identity system");
            }

            return userId;
        }
    }
}
