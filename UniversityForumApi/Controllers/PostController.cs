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
        public async Task<IActionResult> CreatePost([FromForm] string title, [FromForm] string content, [FromForm] IFormFile? media)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var post = new Post
            {
                Title = title,
                Content = content,
                Type = "Text", // Giá trị mặc định, có thể bỏ trường Type nếu không cần
                UserId = userId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            // Xử lý file tải lên (nếu có)
            if (media != null)
            {
                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov" };
                var extension = Path.GetExtension(media.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Định dạng file không được hỗ trợ. Chỉ hỗ trợ hình ảnh (jpg, jpeg, png) và video (mp4, mov).");
                }

                // Kiểm tra kích thước file (giới hạn 10MB)
                if (media.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("Kích thước file không được vượt quá 10MB.");
                }

                // Tạo tên file duy nhất
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                // Tạo thư mục nếu chưa tồn tại
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Lưu file vào thư mục
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await media.CopyToAsync(stream);
                }

                // Lưu URL của file vào model
                post.MediaUrl = $"/uploads/{fileName}";
                post.Type = extension == ".mp4" || extension == ".mov" ? "Video" : "Image";
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Bài viết của bạn đang chờ kiểm duyệt", PostId = post.Id });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] string title, [FromForm] string content, [FromForm] IFormFile? media)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var post = await _context.Posts.FindAsync(id);

            if (post == null || post.UserId != userId)
                return Forbid("Bạn không có quyền chỉnh sửa bài viết này");

            post.Title = title;
            post.Content = content;
            post.UpdatedAt = DateTime.UtcNow;

            // Xử lý file tải lên (nếu có)
            if (media != null)
            {
                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".mp4", ".mov" };
                var extension = Path.GetExtension(media.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Định dạng file không được hỗ trợ. Chỉ hỗ trợ hình ảnh (jpg, jpeg, png) và video (mp4, mov).");
                }

                // Kiểm tra kích thước file (giới hạn 10MB)
                if (media.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("Kích thước file không được vượt quá 10MB.");
                }

                // Xóa file cũ nếu có
                if (!string.IsNullOrEmpty(post.MediaUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.MediaUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Tạo tên file duy nhất
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                // Tạo thư mục nếu chưa tồn tại
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Lưu file mới
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await media.CopyToAsync(stream);
                }

                // Cập nhật URL của file
                post.MediaUrl = $"/uploads/{fileName}";
                post.Type = extension == ".mp4" || extension == ".mov" ? "Video" : "Image";
            }

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

            // Xóa file nếu có
            if (!string.IsNullOrEmpty(post.MediaUrl))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.MediaUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

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
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    MediaUrl = p.MediaUrl, // Thêm trường này
                    Author = p.User.FullName,
                    CreatedAt = p.CreatedAt,
                    LikeCount = p.Likes.Count,
                    Comments = p.Comments.Select(c => new
                    {
                        Id = c.Id,
                        Content = c.Content,
                        Author = c.User.FullName,
                        CreatedAt = c.CreatedAt
                    }).ToList(),
                    UserLiked = p.Likes.Any(l => l.UserId == userId),
                    IsAuthor = true
                })
                .ToListAsync();
            return Ok(posts);
        }

        // Hide a post (user or admin)
        [HttpPut("{id}/hide")]
        [Authorize]
        public async Task<IActionResult> HidePost(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
                return NotFound("Bài viết không tồn tại");

            if (post.UserId != userId && !User.IsInRole("Admin"))
                return Forbid("Bạn không có quyền ẩn bài viết này");

            post.IsHidden = true;
            await _context.SaveChangesAsync();

            return Ok("Bài viết đã được ẩn");
        }

        // Unhide a post (user or admin)
        [HttpPut("{id}/unhide")]
        [Authorize]
        public async Task<IActionResult> UnhidePost(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
                return NotFound("Bài viết không tồn tại");

            if (post.UserId != userId && !User.IsInRole("Admin"))
                return Forbid("Bạn không có quyền hiển thị lại bài viết này");

            post.IsHidden = false;
            await _context.SaveChangesAsync();

            return Ok("Bài viết đã được hiển thị lại");
        }

        // Update the GetPosts method to exclude hidden posts
        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] string? keyword = null)
        {
            var userId = User?.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;

            var query = _context.Posts
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Where(p => p.Status == "Approved" && !p.IsHidden); // Exclude hidden posts

            // Thêm logic tìm kiếm nếu có từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(lowerKeyword) || p.Content.ToLower().Contains(lowerKeyword));
            }

            var posts = await query
                .Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    MediaUrl = p.MediaUrl, // Thêm trường này
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
                    UserLiked = userId.HasValue && p.Likes.Any(l => l.UserId == userId),
                    IsAuthor = userId.HasValue && p.UserId == userId
                })
                .ToListAsync();

            return Ok(posts);
        }

        // Update the GetPostById method to exclude hidden posts
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            var userId = User?.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Where(p => p.Id == id && p.Status == "Approved" && !p.IsHidden) // Exclude hidden posts
                .Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    MediaUrl = p.MediaUrl, // Thêm trường này
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
                    UserLiked = userId.HasValue && p.Likes.Any(l => l.UserId == userId),
                    IsAuthor = userId.HasValue && p.UserId == userId
                })
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return NotFound("Bài viết không tồn tại hoặc chưa được duyệt");
            }

            return Ok(post);
        }

        // Get hidden posts for the authenticated user
        [HttpGet("user/hidden")]
        [Authorize]
        public async Task<IActionResult> GetUserHiddenPosts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var posts = await _context.Posts
                .Where(p => p.UserId == userId && p.IsHidden)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    MediaUrl = p.MediaUrl,
                    Author = p.User.FullName,
                    CreatedAt = p.CreatedAt,
                    LikeCount = p.Likes.Count,
                    Comments = p.Comments.Select(c => new
                    {
                        Id = c.Id,
                        Content = c.Content,
                        Author = c.User.FullName,
                        CreatedAt = c.CreatedAt
                    }).ToList(),
                    UserLiked = p.Likes.Any(l => l.UserId == userId),
                    IsAuthor = true
                })
                .ToListAsync();
            return Ok(posts);
        }
    }
}