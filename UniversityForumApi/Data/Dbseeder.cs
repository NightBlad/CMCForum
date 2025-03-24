using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UniversityForumApi.Models;

namespace UniversityForumApi.Data
{
    public static class DbSeeder
    {
        public static void SeedAdmin(ForumDbContext context)
        {
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var adminUser = new User
                {
                    FullName = "Admin User",
                    DateOfBirth = new DateTime(1980, 1, 1),
                    Contact = "admin@example.com",
                    Role = "Admin",
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123") // Use a secure password hashing method
                };

                context.Users.Add(adminUser);
                context.SaveChanges();
            }
        }
    }
}
