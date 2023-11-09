using Core.Entities.IdentityEntities;
using Core.Interfaces.TokenValidationInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Core.Interfaces;
using Core.Dtos;
using Core.Payloads;

namespace API.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ICalendarService _calendarService;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            ITokenService tokenService, ICalendarService calendarService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _calendarService = calendarService;
        }

        [HttpGet("user")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(userEmail);
            return new UserDto()
            {
                DisplayName = user.UserName,
                Email = user.Email
            };
        }

        [HttpGet("checkemailexists")]
        public async Task<ActionResult<bool>> CheckIfEmailExistsAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (CheckIfEmailExistsAsync(registerDto.Email).Result.Value)
            {
                return BadRequest("Email is already in use");
            }
            else
            {
                var user = new AppUser()
                {
                    Email = registerDto.Email,
                    DisplayName = registerDto.DisplayName,
                    UserName = registerDto.DisplayName,
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(new BadRequestResult());
                }
                else
                {
                    return new UserDto
                    {
                        Email = registerDto.Email,
                        Token = _tokenService.CreateToken(user),
                        DisplayName = registerDto.DisplayName
                    };
                }
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Unauthorized(new UnauthorizedAccessException());
            }
            else
            {
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                if (!result.Succeeded)
                {
                    return Unauthorized(new UnauthorizedAccessException());
                }
                else
                {
                    return new UserDto
                    {
                        Email = user.Email,
                        DisplayName = user.DisplayName,
                        Token = _tokenService.CreateToken(user)
                    };
                }
            }
        }
        
        [HttpGet("authenticate-google")]
        public IActionResult AuthenticateGoogle()
        {
            return Redirect(_calendarService.GetAuthCode());
        }
        
        [HttpGet("auth/google")]
        public async Task<IActionResult> GoogleAuthenticationCallback([FromQuery] string code)
        {
            var tokenResponse = await _calendarService.GetTokens(code);

            return Ok(tokenResponse);
        }
        
        [HttpPost("auth/google/save-refresh-token")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> SaveGoogleRefreshToken([FromBody] SaveGoogleRefreshTokenPayload payload)
        {
            var userDto = await _calendarService.SaveGoogleRefreshToken(payload, GetUserId());

            return Ok(userDto);
        }
    }
}