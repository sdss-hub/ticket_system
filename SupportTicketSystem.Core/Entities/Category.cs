using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.Core.Entities
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public int? ParentCategoryId { get; set; }
        public int Level { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}