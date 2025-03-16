namespace UniversityForumApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Contact { get; set; }
        public string Role { get; set; } // "Student" hoặc "Admin"
        public string Username { get; set; }
        public string PasswordHash { get; set; }
    }
}
