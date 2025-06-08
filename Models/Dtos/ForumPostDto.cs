using System;
using System.ComponentModel.DataAnnotations;

namespace HobbyGeneratorAPI.Models.Dtos
{
    public class ForumPostDto
    {
        public int? Id { get; set; }
        public int? HobbyId { get; set; }
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 1000 characters")]

        public string Content { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}