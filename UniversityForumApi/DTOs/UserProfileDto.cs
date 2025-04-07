using System.ComponentModel.DataAnnotations;

namespace UniversityForumApi.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Contact { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
    }

    public class UpdateUserProfileDto
    {
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        [Required]
        [Phone]
        public string Contact { get; set; }
    }
}
