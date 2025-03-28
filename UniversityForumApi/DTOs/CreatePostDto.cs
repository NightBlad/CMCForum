namespace UniversityForumApi.DTOs
{
    public class CreatePostDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; } // "Text" hoặc "Video" hoặc "Image"
    }
}
