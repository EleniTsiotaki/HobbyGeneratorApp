using System.ComponentModel.DataAnnotations;

namespace HobbyGeneratorAPI.Models.Dtos
{
    public class HobbyCreateDto
    {
        [Required(ErrorMessage = "Hobby name is required")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hobby category is required")]
        public string Type { get; set; } = string.Empty;

        public string? Link { get; set; }

        public string? ImageUrl { get; set; }
    }
}