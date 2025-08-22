using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Core.Interfaces
{
    public interface IAIService
    {
        Task<string> CategorizeSupportTicketAsync(string title, string description);
        Task<int> AnalyzePriorityAsync(string title, string description);
        Task<string> GenerateResponseSuggestionAsync(string ticketContent, string customerMessage);
        Task<double> AnalyzeSentimentAsync(string text);
        Task<int> SuggestBestAgentAsync(string ticketContent, IEnumerable<User> availableAgents);
        Task<AIInsight> CreateInsightAsync(int ticketId, string insightType, object data, double confidence);
    }
}
