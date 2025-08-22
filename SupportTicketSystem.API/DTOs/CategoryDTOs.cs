using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.API.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public CategoryDto? ParentCategory { get; set; }
        public List<CategoryDto> SubCategories { get; set; } = new();
    }

    public class CreateCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
    }

    public class UpdateCategoryDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool? IsActive { get; set; }
    }
}
