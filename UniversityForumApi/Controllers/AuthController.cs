using BCrypt.Net;
using Microsoft.AspNetCore.Authorization; // Thêm namespace này
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UniversityForumApi.DTOs;
using UniversityForumApi.Models;

namespace UniversityForumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ForumDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ForumDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (existingUser != null)
                return BadRequest("Username already exists");

            var user = new User
            {
                FullName = dto.FullName,
                DateOfBirth = dto.DateOfBirth,
                Contact = dto.Contact,
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Student"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid username or password");

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token, Role = user.Role });
        }

        [HttpGet("info")]
        [Authorize] // Chỉ người dùng đã đăng nhập mới truy cập được
        public async Task<IActionResult> GetUserInfo()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            return Ok(new
            {
                fullName = user.FullName,
                dateOfBirth = user.DateOfBirth,
                contact = user.Contact,
                role = user.Role
            });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}