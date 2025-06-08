using System.ComponentModel.DataAnnotations;

namespace HobbyGeneratorAPI.Models.Dtos
{
    public class HobbyDto
    {

        [Required(ErrorMessage = "Hobby name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Hobby name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string Type { get; set; } = string.Empty;

        [Url(ErrorMessage = "Please provide a valid URL")]
        public string Link { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
    }
}