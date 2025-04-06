using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityForumApi.Models;
using UniversityForumApi.DTOs;
namespace UniversityForumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ admin mới truy cập được
    public class AdminController(ForumDbContext context) : ControllerBase
    {
        private readonly ForumDbContext _context = context;

        // Xem danh sách bài viết chờ kiểm duyệt
        [HttpGet("posts/pending")]
        public async Task<IActionResult> GetPendingPosts()
        {
            var posts = await _context.Posts
                .Where(p => p.Status == "Pending")
                .Include(p => p.User)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    p.Type,
                    p.CreatedAt,
                    p.MediaUrl,
                    Author = p.User.FullName
                })
                .ToListAsync();

            return Ok(posts);
        }

        // Duyệt bài viết
        [HttpPut("posts/{id}/approve")]
        public async Task<IActionResult> ApprovePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null || post.Status != "Pending")
                return NotFound("Bài viết không tồn tại hoặc đã được xử lý");

            post.Status = "Approved";

            // Tạo thông báo
            var notification = new Notification
            {
                UserId = post.UserId,
                Content = "Bài viết của bạn đã được duyệt",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Ok("Bài viết đã được duyệt");
        }

        // Từ chối bài viết
        [HttpPut("posts/{id}/reject")]
        public async Task<IActionResult> RejectPost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null || post.Status != "Pending")
                return NotFound("Bài viết không tồn tại hoặc đã được xử lý");

            post.Status = "Rejected";

            // Tạo thông báo
            var notification = new Notification
            {
                UserId = post.UserId,
                Content = "Bài viết của bạn đã bị từ chối",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Ok("Bài viết đã bị từ chối");
        }
        // GET: api/Admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Username,
                    u.DateOfBirth,
                    u.Contact,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/Admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Username,
                user.DateOfBirth,
                user.Contact,
                user.Role
            });
        }

        // POST: api/Admin/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == userDto.Username))
                return BadRequest("Username already exists");

            // Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var user = new User
            {
                FullName = userDto.FullName,
                DateOfBirth = userDto.DateOfBirth,
                Contact = userDto.Contact,
                Role = userDto.Role,
                Username = userDto.Username,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
            {
                user.Id,
                user.FullName,
                user.Username,
                user.DateOfBirth,
                user.Contact,
                user.Role
            });
        }

        // PUT: api/Admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound("User not found");

            // Check if username is being changed and already exists
            if (userDto.Username != user.Username &&
                await _context.Users.AnyAsync(u => u.Username == userDto.Username))
                return BadRequest("Username already exists");

            user.FullName = userDto.FullName;
            user.DateOfBirth = userDto.DateOfBirth;
            user.Contact = userDto.Contact;
            user.Role = userDto.Role;
            user.Username = userDto.Username;

            // Update password if provided
            if (!string.IsNullOrEmpty(userDto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Username,
                user.DateOfBirth,
                user.Contact,
                user.Role
            });
        }

        // DELETE: api/Admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound("User not found");

            // Prevent deleting the last admin account
            if (user.Role == "Admin" &&
                await _context.Users.CountAsync(u => u.Role == "Admin") <= 1)
                return BadRequest("Cannot delete the last admin account");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("User deleted successfully");
        }
    }
}

    