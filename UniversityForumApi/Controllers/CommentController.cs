using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniversityForumApi.DTOs;
using UniversityForumApi.Models;

namespace UniversityForumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public CommentController(ForumDbContext context)
        {
            _context = context;
        }

        // Thêm bình luận (chỉ người đăng nhập mới dùng được)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var post = await _context.Posts.FindAsync(dto.PostId);
                if (post == null || post.Status != "Approved")
                {
                    return NotFound("Bài viết không tồn tại hoặc chưa được duyệt");
                }

                var commenter = await _context.Users.FindAsync(userId);
                if (commenter == null)
                {
                    return NotFound("Không tìm thấy người bình luận");
                }

                var comment = new Comment
                {
                    Content = dto.Content,
                    PostId = dto.PostId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);

                // Tạo thông báo cho người đăng bài
                var notification = new Notification
                {
                    UserId = post.UserId,
                    Content = $"{commenter.FullName} đã bình luận bài viết của bạn: \"{post.Title}\"",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Bình luận đã được thêm", CommentId = comment.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Đã xảy ra lỗi: {ex.Message}");
            }
        }

        // Xem bình luận của bài viết
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetComments(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.Status != "Approved")
                return NotFound("Bài viết không tồn tại hoặc chưa được duyệt");

            var comments = await _context.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    Author = c.User.FullName
                })
                .ToListAsync();

            return Ok(comments);
        }
    }
}