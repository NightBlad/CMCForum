namespace UniversityForumApi.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public List<Like> Likes { get; set; } = new List<Like>();
        public string? MediaUrl { get; set; } // Thêm trường này để lưu URL của file, có thể null
    }
}