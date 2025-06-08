using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace HobbyGeneratorAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public List<Hobby> Hobbies { get; set; } = new();
        public List<ForumPost> ForumPosts { get; set; } = new();
    }
}