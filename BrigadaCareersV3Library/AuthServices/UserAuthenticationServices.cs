using BrigadaCareersV3Library.ApiResponseMessage;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.Dto.AuthDto;
using BrigadaCareersV3Library.Dto.UserDto;
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
        //private readonly BrigadaCanteenContext _context;
        private readonly UserManager<ApplicationIdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public UserAuthenticationService(UserManager<ApplicationIdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;

        }

        public async Task<string> RegisteredUser(UserDto register)
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
                    Email = ""
                };


                var result = await _userManager.CreateAsync(user, register.Password!);

                if (!result.Succeeded)
                {
                    //return "Error : Please Try Again";
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


                //FOR USER ROLE
                //if (await _roleManager.RoleExistsAsync(UserRole.User))
                //{
                //    await _userManager.AddToRoleAsync(user, UserRole.User);
                //}

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
                //var loginUser = "";

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
                    var generatedToken = new SymmetricSecurityKey(await _userManager.CreateSecurityTokenAsync(loginUser));
                    var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecreteKey"]!));
                    var role = "";
                    var userRole = await _userManager.GetRolesAsync(loginUser);

                    foreach (var userrole in userRole)
                    {
                        role = userrole + ",";
                    }

                    await _userManager.RemoveAuthenticationTokenAsync(loginUser, "userIdentity", role);
                    var newRefreshToken = await _userManager.GenerateUserTokenAsync(loginUser, "userIdentity", role);
                    await _userManager.SetAuthenticationTokenAsync(loginUser, "userIdentity", role, newRefreshToken);


                    var authClaim = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier,loginUser.Id),
                        new Claim(ClaimTypes.GivenName,""),
                        new Claim(ClaimTypes.Name,loginUser.UserName!),
                        new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                    };

                    foreach (var userrole in userRole)
                    {
                        authClaim.Add(new Claim(ClaimTypes.Role, userrole));
                    }

                    var token = new JwtSecurityToken
                        (
                            issuer: _configuration["JWT:ValidIssuer"],
                            audience: _configuration["JWT:ValidAudience"],
                            expires: DateTime.UtcNow.AddHours(8),
                            claims: authClaim,
                            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                        );

                    var userInfo = new UserLoginDto
                    {
                        userID = loginUser.Id,
                        UserToken = new JwtSecurityTokenHandler().WriteToken(token),
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
        //public async Task<ApiResponseMessage<IList<GetAllRolesDto>>> GetAllRoles()
        //{
        //    try
        //    {

        //        var getRoles = await _context.AspNetRoles.Select(x => new GetAllRolesDto
        //        {
        //            UserRoleName = x.Name!
        //        }).ToListAsync();

        //        return new ApiResponseMessage<IList<GetAllRolesDto>>
        //        {
        //            Data = getRoles ?? [],
        //            IsSuccess = getRoles is not null ? true : false,
        //            ErrorMessage = ""
        //        };
        //    }
        //    catch (Exception ex)
        //    {

        //        return new ApiResponseMessage<IList<GetAllRolesDto>>
        //        {
        //            Data = null!,
        //            IsSuccess = false,
        //            ErrorMessage = ex.Message
        //        };
        //    }
        //}
        //public async Task<ApiResponseMessage<IList<GetAllUserDto>>> GetAllUser(PagedSortedDto dto)
        //{
        //    try
        //    {

        //        IQueryable<TblUser> datares = _context.TblUsers;


        //        var query = await (from user in _context.TblUsers
        //                           join aspNetUser in _context.AspNetUsers
        //                           on user.Userid.ToString() equals aspNetUser.Id
        //                           where string.IsNullOrEmpty(dto.searchstring) ||
        //                                 user.FirstName.Contains(dto.searchstring) ||
        //                                 user.LastName.Contains(dto.searchstring)
        //                           select new
        //                           {
        //                               user,
        //                               aspNetUser,
        //                               Roles = aspNetUser.Roles.Select(role => role.Name).ToList()
        //                           })
        //                          .Skip(dto.pageIndex * dto.pageSize)
        //                          .Take(dto.pageSize)
        //                          .ToListAsync();

        //        var totalRecords = await (from user in _context.TblUsers
        //                                  join aspNetUser in _context.AspNetUsers
        //                                  on user.Userid.ToString() equals aspNetUser.Id
        //                                  where string.IsNullOrEmpty(dto.searchstring) ||
        //                                        user.FirstName.Contains(dto.searchstring) ||
        //                                        user.LastName.Contains(dto.searchstring)
        //                                  select user).CountAsync();

        //        var result = query.Select(x => new GetAllUserDto
        //        {
        //            CustomerId = x.user.CustomerId,
        //            UserId = x.aspNetUser.Id,
        //            FirstName = x.user.FirstName,
        //            LastName = x.user.LastName,
        //            Email = x.user.Email,
        //            PhoneNumber = x.user.PhoneNumber,
        //            Gender = x.user.Gender,
        //            Birthdate = x.user.Birthdate.ToString(),
        //            CivilStatus = x.user.CivilStatus,
        //            TotalRecords = totalRecords,
        //            Roles = x.Roles
        //        }).ToList();



        //        return new ApiResponseMessage<IList<GetAllUserDto>>
        //        {

        //            Data = result! ?? [],
        //            IsSuccess = query is not null ? true : false,
        //            ErrorMessage = ""
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponseMessage<IList<GetAllUserDto>>
        //        {
        //            Data = null!,
        //            IsSuccess = false,
        //            ErrorMessage = ex.Message
        //        };


        //    }
        //}
        //public async Task<ApiResponseMessage<string>> AddOrRemoveRoles(AddOrRemoveRolesDto dto)
        //{
        //    try
        //    {

        //        var apiMessage = "";
        //        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());

        //        switch (dto.ButtonClick)
        //        {
        //            case 1:

        //                if (await _roleManager.RoleExistsAsync(dto.Role))
        //                {
        //                    await _userManager.AddToRoleAsync(user!, dto.Role);

        //                    apiMessage = dto.Role + " Role Added";
        //                }

        //                break;

        //            case 2:

        //                if (await _roleManager.RoleExistsAsync(dto.Role))
        //                {
        //                    await _userManager.RemoveFromRoleAsync(user!, dto.Role);

        //                    apiMessage = dto.Role + " Role Remove";
        //                }

        //                break;
        //        }



        //        return new ApiResponseMessage<string>
        //        {
        //            Data = apiMessage!,
        //            IsSuccess = true,
        //            ErrorMessage = ""
        //        };
        //    }
        //    catch (Exception ex)
        //    {

        //        return new ApiResponseMessage<string>
        //        {
        //            Data = null!,
        //            IsSuccess = false,
        //            ErrorMessage= ex.Message
        //        };
        //    }
        //}
        //public async Task<ApiResponseMessage<IList<GetAvaialbleRolesDto>>> GetAvaialbleRoles(Guid id)
        //{
        //    try
        //    {

        //        var queryRoles = await (from user in _context.TblUsers
        //                                join aspNetUser in _context.AspNetUsers
        //                                on user.Userid.ToString() equals aspNetUser.Id
        //                                where aspNetUser.Id == id.ToString()
        //                                select aspNetUser.Roles.Select(role => role.Name))
        //                                .FirstOrDefaultAsync() ?? new List<string>();


        //        var allRoles = await _context.AspNetRoles
        //            .Select(role => role.Name)
        //            .ToListAsync();

        //        var missingRoles = allRoles.Except(queryRoles).ToList();


        //        var result = missingRoles.Select(role => new GetAvaialbleRolesDto
        //        {
        //            RoleName = new List<string> { role }
        //        }).ToList();

        //        return new ApiResponseMessage<IList<GetAvaialbleRolesDto>>
        //        {
        //            Data = result,
        //            IsSuccess = true,
        //            ErrorMessage = ""
        //        };

        //    }
        //    catch (Exception ex)
        //    {

        //        return new ApiResponseMessage<IList<GetAvaialbleRolesDto>>
        //        { 
        //        Data = null!,
        //        IsSuccess = false,
        //        ErrorMessage = ex.Message
        //        };
        //    }
        //}
    }
}
