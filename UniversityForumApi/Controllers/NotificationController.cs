using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniversityForumApi.Models;

namespace UniversityForumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController(ForumDbContext context) : ControllerBase
    {
        private readonly ForumDbContext _context = context;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Content,
                    n.IsRead,
                    n.CreatedAt,
                    n.PostId // Thêm PostId vào response
                })
                .ToListAsync();

            return Ok(notifications);
        }
    }
}