using Microsoft.AspNetCore.Mvc;
using WebAPI.Application.Services;
using WebAPI.Infrastructure.Repositories;
using WebAPI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Common;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(IUserRepository userRepository, JwtTokenService jwtTokenService)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepository.GetByUsername(request.Username);
            if (user == null || !VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized();
            }
            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new { Token = token });
        }

        // Example password verification (replace with your hashing logic)
        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            // TODO: Use a secure hash comparison
            return PasswordHelper.HashPassword(inputPassword).ToLower() == storedHash.ToLower();
        }
    }

    public class LoginRequest
    {
        //[Required]
        //public string ClientId { get; set; } = string.Empty;
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}