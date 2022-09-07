using JWT_Authetication_Example.Models;
using JWT_Authetication_Example.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JWT_Authetication_Example.Controllers
{
    
    [ApiController]
    [Route("[action]")]
    public class UserAuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public UserAuthController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {

            _userManager = userManager;
            _configuration = configuration;
        }

       
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserLogin login)
        {
            try
            {

                var user = await _userManager.FindByNameAsync(login.Username);
                if (user != null && await _userManager.CheckPasswordAsync(user, login.Password))
                {
                    var userRoles = await _userManager.GetRolesAsync(user);

                    var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                    foreach (var userRole in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                    }

                    var token = GetToken(authClaims);
                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        expiration = token.ValidTo

                    });
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                var response = new ServiceResponse();
                response.Success = false;
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] UserRegistration registration)
        {

            try
            {
                var userExist = await _userManager.FindByNameAsync(registration.Username);
                if (userExist != null)
                    return StatusCode(StatusCodes.Status500InternalServerError, "User wit this username already exists!");

                IdentityUser user = new()
                {
                    Email = registration.Email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = registration.Username
                };

                var result = await _userManager.CreateAsync(user, registration.Password);

                if (!result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create user, please try again.");
                }

                return Ok("User Created Successfully!");

            }
            catch (Exception ex )
            {
                var response = new ServiceResponse();
                response.Success = false;
                response.Message = ex.Message;
                return BadRequest(response);
            }

        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetByName([FromBody] UserLogin get)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(get.Username);
                if(user== null) 
                    return NotFound();  

                return Ok(user);
            }
            catch (Exception ex)
            {
                var response = new ServiceResponse();
                response.Success = false;
                response.Message = ex.Message;
                return BadRequest(response);
            }
       

        }
        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:key"]));
            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                authClaims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signIn);

            return token;
        }

    }
}
