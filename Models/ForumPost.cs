using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HobbyGeneratorAPI.Models
{
    public class ForumPost
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!; // Navigation property

        public int HobbyId { get; set; }
        public Hobby Hobby { get; set; } = null!; // Navigation property

        public int? ParentPostId { get; set; } // Nullable, null for top-level posts
        public ForumPost? ParentPost { get; set; } // Navigation to parent post
        public ICollection<ForumPost> Replies { get; set; } = new List<ForumPost>(); // Child replies
    }
}