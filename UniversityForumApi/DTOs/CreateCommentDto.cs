namespace UniversityForumApi.DTOs
{
    public class CreateCommentDto
    {
        public string Content { get; set; }
        public int PostId { get; set; }
    }
}