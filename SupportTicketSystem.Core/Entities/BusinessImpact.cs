namespace SupportTicketSystem.Core.Entities
{
    public class BusinessImpact
    {
        // Is this blocking work?
        public BlockingLevel BlockingLevel { get; set; } = BlockingLevel.NotBlocking;

        // How many people affected?
        public ImpactScope ImpactScope { get; set; } = ImpactScope.Individual;

        // Any urgent deadline?
        public DateTime? UrgentDeadline { get; set; }

        // Customer's perceived urgency (optional context)
        public string? AdditionalContext { get; set; }
    }

    public enum BlockingLevel
    {
        NotBlocking = 1,        
        PartiallyBlocking = 2,  
        CompletelyBlocking = 3, 
        SystemDown = 4    
    }

    public enum ImpactScope
    {
        Individual = 1,
        Team = 2,  
        Department = 3, 
        Company = 4    
    }
}
