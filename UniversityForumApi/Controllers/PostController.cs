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
    public class PostController(ForumDbContext context) : ControllerBase
    {
        private readonly ForumDbContext _context = context;

        // Đăng bài viết (chỉ người đăng nhập mới dùng được)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                UserId = userId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Bài viết của bạn đang chờ kiểm duyệt", PostId = post.Id });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var post = await _context.Posts.FindAsync(id);

            if (post == null || post.UserId != userId)
                return Forbid("Bạn không có quyền chỉnh sửa bài viết này");

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Bài viết đã được cập nhật");
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var post = await _context.Posts.FindAsync(id);

            if (post == null || post.UserId != userId)
                return Forbid("Bạn không có quyền xóa bài viết này");

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return Ok("Bài viết đã được xóa");
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserPosts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var posts = await _context.Posts
                .Where(p => p.UserId == userId && p.Status == "Approved")
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    Author = p.User.FullName,
                    p.CreatedAt,
                    LikeCount = p.Likes.Count,
                    Comments = p.Comments.Select(c => new
                    {
                        c.Id,
                        c.Content,
                        Author = c.User.FullName,
                        c.CreatedAt
                    }).ToList(),
                    UserLiked = p.Likes.Any(l => l.UserId == userId),
                    IsAuthor = true // Luôn true vì đây là bài viết của người dùng hiện tại
                })
                .ToListAsync();
            return Ok(posts);
        }

        // Lấy danh sách bài viết đã duyệt (công khai)
        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] string? keyword = null)
        {
            var userId = User?.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;

            var query = _context.Posts
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Where(p => p.Status == "Approved");

            // Thêm logic tìm kiếm nếu có từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(p => p.Title.Contains(lowerKeyword, StringComparison.CurrentCultureIgnoreCase) || p.Content.Contains(lowerKeyword, StringComparison.CurrentCultureIgnoreCase));
            }

            var posts = await query
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    Author = p.User != null ? p.User.FullName : "Unknown",
                    p.CreatedAt,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0,
                    Comments = p.Comments.Select(c => new
                    {
                        c.Id,
                        c.Content,
                        Author = c.User != null ? c.User.FullName : "Unknown",
                        c.CreatedAt
                    }).ToList(),
                    UserLiked = userId.HasValue && (p.Likes != null && p.Likes.Any(l => l.UserId == userId)),
                    IsAuthor = userId.HasValue && p.UserId == userId
                })
                .ToListAsync();

            return Ok(posts);
        }

        // Lấy chi tiết bài viết theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            var userId = User?.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Where(p => p.Id == id && p.Status == "Approved")
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    Author = p.User != null ? p.User.FullName : "Unknown",
                    p.CreatedAt,
                    LikeCount = p.Likes != null ? p.Likes.Count : 0,
                    Comments = p.Comments.Select(c => new
                    {
                        c.Id,
                        c.Content,
                        Author = c.User != null ? c.User.FullName : "Unknown",
                        c.CreatedAt
                    }).ToList(),
                    UserLiked = userId.HasValue && p.Likes.Any(l => l.UserId == userId),
                    IsAuthor = userId.HasValue && p.UserId == userId // Thêm trường này
                })
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return NotFound("Bài viết không tồn tại hoặc chưa được duyệt");
            }

            return Ok(post);
        }
    }
}