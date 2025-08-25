namespace SupportTicketSystem.Core.Entities
{
    public class Feedback
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int CustomerId { get; set; }
        public int Rating { get; set; } // 1-5 rating scale
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Ticket Ticket { get; set; } = null!;
        public User Customer { get; set; } = null!;
    }
}
