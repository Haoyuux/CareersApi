using Azure;
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
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BrigadaCareersV3Library.AuthServices
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly UserManager<ApplicationIdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly BrigadaCareersDbv3Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserAuthenticationService(UserManager<ApplicationIdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        BrigadaCareersDbv3Context context,
        IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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
                if (isExistUser != null)
                {
                    return "User Already Exists";
                }

                var user = new ApplicationIdentityUser
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = register.UserName,
                    Email = register.Email,
                };

                var result = await _userManager.CreateAsync(user, register.Password!);

                if (result.Succeeded)
                {
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

                    await _context.TblUserDetails.AddAsync(userDetails);
                    await _context.SaveChangesAsync();
                }

                if (!result.Succeeded)
                {
                    var errors = "";
                    foreach (var error in result.Errors)
                    {
                        errors = error.Code + ", " + error.Description;
                    }
                    return ("Error :" + errors);
                }

                if (!await _roleManager.RoleExistsAsync(UserRole.User))
                {
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.User));
                }
                if (await _roleManager.RoleExistsAsync(UserRole.User))
                {
                    await _userManager.AddToRoleAsync(user, UserRole.User);
                }
                return user.Id;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return msg;
            }
        }

        public async Task<string> UpdateUserDetails(UserDto register)
        {
            return null;
        }

        public async Task<string> RegisteredAdmin(RegisterUserDto register)
        {
            try
            {
                var isExistUser = await _userManager.FindByNameAsync(register.UserName);
                if (isExistUser != null)
                {
                    return "User Already Exists";
                }

                var user = new ApplicationIdentityUser
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = register.UserName,
                    Email = ""
                };

                var result = await _userManager.CreateAsync(user, register.Password);

                if (!result.Succeeded)
                {
                    return "Error : Cannot Create Admin --> Please Try Again.";
                }

                if (!await _roleManager.RoleExistsAsync(UserRole.Admin))
                {
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.Admin));
                }
                if (!await _roleManager.RoleExistsAsync(UserRole.User))
                {
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.User));
                }
                if (await _roleManager.RoleExistsAsync(UserRole.Admin))
                {
                    await _userManager.AddToRoleAsync(user, UserRole.Admin);
                }
                return "Success";
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return msg;
            }
        }

        public async Task<ApiResponseMessage<UserLoginDto>> LoginAccount(RegisterUserDto login)
        {
            try
            {
                var usernameExist = await _userManager.FindByNameAsync(login.UserName);
                var emailExist = await _userManager.FindByEmailAsync(login.UserName);

                var loginUser = new ApplicationIdentityUser();

                if (usernameExist != null)
                {
                    loginUser = usernameExist;
                }
                else if (emailExist != null)
                {
                    loginUser = emailExist;
                }

                if (loginUser != null && await _userManager.CheckPasswordAsync(loginUser, login.Password))
                {
                    var userRole = await _userManager.GetRolesAsync(loginUser);

                    // Generate new access token using helper method
                    var accessToken = await GenerateAccessToken(loginUser, userRole);

                    // Generate new refresh token using helper method
                    var newRefreshToken = await GenerateRefreshToken(loginUser);

                    var userInfo = new UserLoginDto
                    {
                        userID = loginUser.Id,
                        UserToken = accessToken,
                        newRefreshToken = newRefreshToken,
                        UserName = loginUser.UserName!,
                        UserRole = userRole.ToList()
                    };

                    var _apiResponse = new ApiResponseMessage<UserLoginDto>
                    {
                        Data = userInfo,
                        IsSuccess = true,
                        ErrorMessage = ""
                    };
                    return _apiResponse;
                }

                var _apiResponse1 = new ApiResponseMessage<UserLoginDto>
                {
                    Data = null!,
                    IsSuccess = false,
                    ErrorMessage = "No User Found --> Try Again"
                };
                return _apiResponse1;
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var _apiResponse1 = new ApiResponseMessage<UserLoginDto>
                {
                    Data = null!,
                    IsSuccess = false,
                    ErrorMessage = message
                };
                return _apiResponse1;
            }
        }

        // NEW: Refresh Token Method
        public async Task<ApiResponseMessage<UserLoginDto>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new ApiResponseMessage<UserLoginDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "Refresh token is required"
                    };
                }

                // Find user by refresh token
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

                // Validate the refresh token
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

                // Get user roles
                var userRoles = await _userManager.GetRolesAsync(user);

                // Generate new access token
                var newAccessToken = await GenerateAccessToken(user, userRoles);

                // Generate new refresh token (token rotation for security)
                var newRefreshToken = await GenerateRefreshToken(user);

                var userInfo = new UserLoginDto
                {
                    userID = user.Id,
                    UserToken = newAccessToken,
                    newRefreshToken = newRefreshToken,
                    UserName = user.UserName!,
                    UserRole = userRoles.ToList()
                };

                return new ApiResponseMessage<UserLoginDto>
                {
                    Data = userInfo,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<UserLoginDto>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // NEW: Invalidate Refresh Token Method
        public async Task<ApiResponseMessage<bool>> InvalidateRefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new ApiResponseMessage<bool>
                    {
                        Data = false,
                        IsSuccess = false,
                        ErrorMessage = "Refresh token is required"
                    };
                }

                var user = await FindUserByRefreshTokenAsync(refreshToken);
                if (user != null)
                {
                    // Remove the refresh token
                    await _userManager.RemoveAuthenticationTokenAsync(user, "userIdentity", "refresh_token");

                    return new ApiResponseMessage<bool>
                    {
                        Data = true,
                        IsSuccess = true,
                        ErrorMessage = ""
                    };
                }

                return new ApiResponseMessage<bool>
                {
                    Data = false,
                    IsSuccess = false,
                    ErrorMessage = "Invalid refresh token"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<bool>
                {
                    Data = false,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // NEW: Logout Method
        public async Task<ApiResponseMessage<bool>> LogoutAsync(string refreshToken)
        {
            try
            {
                var result = new ApiResponseMessage<bool>
                {
                    Data = true,
                    IsSuccess = true,
                    ErrorMessage = ""
                };

                // If refresh token is provided, invalidate it
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var invalidateResult = await InvalidateRefreshTokenAsync(refreshToken);
                    if (!invalidateResult.IsSuccess)
                    {
                        result.ErrorMessage = "Logout successful, but refresh token invalidation failed: " + invalidateResult.ErrorMessage;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<bool>
                {
                    Data = false,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // NEW: Helper method to generate access token
        private async Task<string> GenerateAccessToken(ApplicationIdentityUser user, IList<string> userRoles)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecreteKey"]!));

            var authClaim = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add roles
            foreach (var role in userRoles)
            {
                authClaim.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddHours(8), // Access token expires in 8 hours
                claims: authClaim,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // NEW: Helper method to generate refresh token
        private async Task<string> GenerateRefreshToken(ApplicationIdentityUser user)
        {
            // Remove existing refresh token (if any)
            await _userManager.RemoveAuthenticationTokenAsync(user, "userIdentity", "refresh_token");

            // Generate new refresh token
            var newRefreshToken = await _userManager.GenerateUserTokenAsync(user, "userIdentity", "refresh_token");

            // Store the new refresh token
            await _userManager.SetAuthenticationTokenAsync(user, "userIdentity", "refresh_token", newRefreshToken);

            return newRefreshToken;
        }

        // NEW: Helper method to find user by refresh token
        private async Task<ApplicationIdentityUser?> FindUserByRefreshTokenAsync(string refreshToken)
        {
            var allUsers = await _userManager.Users.ToListAsync();

            foreach (var user in allUsers)
            {
                var storedToken = await _userManager.GetAuthenticationTokenAsync(user, "userIdentity", "refresh_token");
                if (!string.IsNullOrEmpty(storedToken) && storedToken == refreshToken)
                {
                    return user;
                }
            }

            return null;
        }

        // NEW: Helper method to validate refresh token
        private async Task<bool> ValidateRefreshTokenAsync(ApplicationIdentityUser user, string refreshToken)
        {
            try
            {
                var isValid = await _userManager.VerifyUserTokenAsync(user, "userIdentity", "refresh_token", refreshToken);
                return isValid;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ApiResponseMessage<getUserProfileDetailsDto>> getUserProfileDetails()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var identityUser = await _userManager.FindByIdAsync(currentUserId);
                if (identityUser == null)
                {
                    return new ApiResponseMessage<getUserProfileDetailsDto>
                    {
                        Data = null,
                        IsSuccess = false,
                        ErrorMessage = "User not found in identity system"
                    };
                }

                // Get user details from your custom table
                var userDetails = await _context.TblUserDetails
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

                // Get user roles
                var userRoles = await _userManager.GetRolesAsync(identityUser);

                // Map to DTO
                //var profileDto = new getUserProfileDetailsDto
                //{
                //    Id = userDetails.Id,
                //    UserId = userDetails.UserId,
                //    FirstName = userDetails.FirstName,
                //    LastName = userDetails.LastName,
                //    EmailAddress = userDetails.EmailAddress,
                //    UserName = identityUser.UserName,
                //    IsActive = userDetails.IsActive,
                //    CreationTime = userDetails.CreationTime,
                //    UserRoles = userRoles.ToList()
                //};

                return new ApiResponseMessage<getUserProfileDetailsDto>
                {
                    Data = null,
                    IsSuccess = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseMessage<getUserProfileDetailsDto>
                {
                    Data = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public string GetCurrentUserId()
          {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("No HttpContext. User is not authenticated.");

            var user = httpContext.User ?? throw new InvalidOperationException("No User principal on HttpContext.");

            // Common claim types for user id across providers
            string[] idClaimTypes =
            {
                ClaimTypes.NameIdentifier,                 // "…/nameidentifier"
                JwtRegisteredClaimNames.Sub,               // "sub"
                "sub",                                     // raw sub
                "oid",                                     // AAD object id
                "uid", "userid", "user_id", "id", "nameid" // custom possibilities
            };

            var userId = idClaimTypes
                .Select(t => user.FindFirstValue(t))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            // Debug: log all claims once if needed
            if (string.IsNullOrWhiteSpace(userId))
            {
                var available = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));
                throw new InvalidOperationException(
                    "User ID claim is missing from the authentication token. Available claims: " + available);
            }

            return userId;
        }
    }
}