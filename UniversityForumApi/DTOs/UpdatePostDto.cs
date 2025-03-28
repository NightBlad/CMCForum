namespace UniversityForumApi.DTOs
{
    public class UpdatePostDto
    {
        public string Title { get; set; }
        public string Content { get; set; }

        // Thêm trường này để cập nhật Type
        public string Type { get; set; }
    }
}