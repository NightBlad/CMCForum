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
    public class PostController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public PostController(ForumDbContext context)
        {
            _context = context;
        }

        // Đăng bài viết (chỉ người đăng nhập mới dùng được)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            // Lấy UserId từ token JWT
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                UserId = userId,
                Status = "Pending", // Trạng thái mặc định
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Bài viết của bạn đang chờ kiểm duyệt", PostId = post.Id });
        }

        // Lấy danh sách bài viết đã duyệt (công khai)
        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            var userId = User?.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;

            var posts = await _context.Posts
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Where(p => p.Status == "Approved")
                .Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    Author = p.User != null ? p.User.FullName : "Unknown",
                    CreatedAt = p.CreatedAt,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0,
                    Comments = p.Comments.Select(c => new
                    {
                        Id = c.Id,
                        Content = c.Content,
                        Author = c.User != null ? c.User.FullName : "Unknown",
                        CreatedAt = c.CreatedAt
                    }).ToList(),
                    UserLiked = userId.HasValue && p.Likes.Any(l => l.UserId == userId) // Kiểm tra xem người dùng đã thích chưa
                })
                .ToListAsync();

            return Ok(posts);
        }
    }
}