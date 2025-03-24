using System;
using System.ComponentModel.DataAnnotations;

namespace UniversityForumApi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Phone]
        public string Contact { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // "Student" or "Admin"

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }
    }
}
