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
        public List<Comment> Comments { get; set; } = new List<Comment>(); // Đã thêm từ bước trước
        public List<Like> Likes { get; set; } = new List<Like>(); // Thêm để lưu danh sách Likes
    }
}