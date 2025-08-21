using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.Core.Entities
{
    public class Attachment
    {
        public int Id { get; set; }
        
        public int? TicketId { get; set; }
        public int? CommentId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? FilePath { get; set; } 
        
        public byte[]? FileData { get; set; } 
        
        public int UploadedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual Ticket? Ticket { get; set; }
        public virtual TicketComment? Comment { get; set; }
        public virtual User UploadedBy { get; set; } = null!;
    }
}