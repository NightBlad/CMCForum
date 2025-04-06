using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UniversityForumApi.Data;
using UniversityForumApi.Models;
using UniversityForumApi.DTOs;

namespace UniversityForumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public UserController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: api/User/Profile
        [HttpGet("Profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                Contact = user.Contact,
                Role = user.Role,
                Username = user.Username
            };
        }

        // PUT: api/User/UpdateProfile
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile(UpdateUserProfileDto profileDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.FullName = profileDto.FullName;
            user.DateOfBirth = profileDto.DateOfBirth;
            user.Contact = profileDto.Contact;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Thông tin cá nhân đã được cập nhật thành công" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật thông tin cá nhân" });
            }
        }
        [HttpPut("UpdateLogin")]
        public async Task<IActionResult> UpdateLogin(UpdateUserLoginDto loginDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Verify current password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(loginDto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
            }

            // Check if the new username already exists (if username is being changed)
            if (loginDto.Username != user.Username)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Tên đăng nhập đã tồn tại, vui lòng chọn tên khác" });
                }
            }

            // Update username
            user.Username = loginDto.Username;

            // Update password if provided
            if (!string.IsNullOrEmpty(loginDto.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginDto.NewPassword);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Thông tin đăng nhập đã được cập nhật thành công" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật thông tin đăng nhập" });
            }
        }

        [HttpGet("CheckUsername")]
        public async Task<ActionResult<bool>> CheckUsername(string username)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Id != userId);

            return existingUser != null;
        }
    }
}
