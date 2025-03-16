using Microsoft.EntityFrameworkCore;

namespace UniversityForumApi.Models
{
    public class ForumDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }

        public ForumDbContext(DbContextOptions<ForumDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình khóa chính ghép cho Likes
            modelBuilder.Entity<Like>()
                .HasKey(l => new { l.UserId, l.PostId });

            // Cấu hình mối quan hệ với User (không dùng Cascade để tránh xung đột)
            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany() // User không cần danh sách Likes cụ thể
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình mối quan hệ với Post
            modelBuilder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ cho Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
