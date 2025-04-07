using System.ComponentModel.DataAnnotations;

namespace UniversityForumApi.DTOs
{
    public class RegisterDto
    {
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        [Required]
        [Phone]
        public string Contact { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
