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
    }
}
