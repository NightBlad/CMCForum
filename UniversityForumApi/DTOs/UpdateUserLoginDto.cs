namespace UniversityForumApi.DTOs
{
    public class UpdateUserLoginDto
    {
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
