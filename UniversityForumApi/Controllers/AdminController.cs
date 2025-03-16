using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityForumApi.Models;

namespace UniversityForumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ admin mới truy cập được
    public class AdminController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public AdminController(ForumDbContext context)
        {
            _context = context;
        }

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
    }
}