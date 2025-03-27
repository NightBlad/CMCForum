using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniversityForumApi.Models;

namespace UniversityForumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LikeController(ForumDbContext context) : ControllerBase
    {
        private readonly ForumDbContext _context = context;

        // Thích hoặc bỏ thích bài viết
        [HttpPost("post/{postId}")]
        [Authorize]
        public async Task<IActionResult> LikePost(int postId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found"));

                var post = await _context.Posts.FindAsync(postId);
                if (post == null || post.Status != "Approved")
                {
                    return NotFound("Bài viết không tồn tại hoặc chưa được duyệt");
                }

                var existingLike = await _context.Likes
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);

                if (existingLike == null)
                {
                    // Thêm lượt thích
                    var like = new Like
                    {
                        UserId = userId,
                        PostId = postId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Likes.Add(like);

                    var postOwner = await _context.Users.FindAsync(post.UserId);
                    var liker = await _context.Users.FindAsync(userId);
                    var notification = new Notification
                    {
                        UserId = post.UserId,
                        Content = $"{liker.FullName} đã thích bài viết của bạn: \"{post.Title}\"",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        PostId = postId
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "Đã thích bài viết", Action = "Liked" });
                }
                else
                {
                    // Bỏ thích
                    _context.Likes.Remove(existingLike);
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "Đã bỏ thích bài viết", Action = "Unliked" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Đã xảy ra lỗi: {ex.Message}");
            }
        }

        // Bỏ thích bài viết (giữ lại để hỗ trợ DELETE nếu cần)
        [HttpDelete("post/{postId}")]
        [Authorize]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found"));

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);

            if (like == null)
                return NotFound("Bạn chưa thích bài viết này");

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok("Đã bỏ thích bài viết");
        }
    }
}