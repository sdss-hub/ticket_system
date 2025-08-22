using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;

namespace SupportTicketSystem.Infrastructure.Services
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? "demo-key";
            _model = configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
            
            if (_apiKey != "demo-key")
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            }
        }

        public async Task<string> CategorizeSupportTicketAsync(string title, string description)
        {
            if (_apiKey == "demo-key")
            {
                return AnalyzeCategoryOffline(title, description);
            }

            var prompt = $@"Analyze this support ticket and categorize it into one of these categories:
- Technical Issue
- Account Problem  
- Billing Question
- Feature Request
- Bug Report
- General Inquiry

Title: {title}
Description: {description}

Return only the category name, nothing else.";

            var response = await CallOpenAIAsync(prompt);
            return response?.Trim() ?? AnalyzeCategoryOffline(title, description);
        }

        public async Task<int> AnalyzePriorityAsync(string title, string description, BusinessImpact? businessImpact = null)
        {
            // First, analyze business impact if provided
            var businessPriority = AnalyzeBusinessImpact(businessImpact);
            
            if (_apiKey == "demo-key")
            {
                var contentPriority = AnalyzePriorityOffline(title, description);
                return Math.Max(businessPriority, contentPriority);
            }

            var prompt = $@"Analyze this support ticket and determine its priority level:
1 = Low (general questions, minor issues, no urgency)
2 = Medium (moderate impact issues, standard business hours)  
3 = High (significant impact, affects productivity, needs quick response)
4 = Critical (system down, major blocker, revenue impacting, many users affected)

Consider these factors:
- Keywords indicating urgency: 'urgent', 'critical', 'down', 'broken', 'can't work', 'blocking'
- Impact words: 'all users', 'system wide', 'production', 'revenue', 'customers affected'
- Emotion indicators: 'frustrated', 'angry', multiple exclamation marks
- Time sensitivity: 'ASAP', 'immediately', 'deadline', 'meeting in 1 hour'

Title: {title}
Description: {description}

Business Context: {(businessImpact != null ? $"Blocking Level: {businessImpact.BlockingLevel}, Impact Scope: {businessImpact.ImpactScope}" : "None provided")}

Return only the priority number (1-4), nothing else.";

            var response = await CallOpenAIAsync(prompt);
            
            if (int.TryParse(response?.Trim(), out int aiPriority) && aiPriority >= 1 && aiPriority <= 4)
            {
                // Return the higher of business impact or AI analysis
                return Math.Max(businessPriority, aiPriority);
            }
            
            return Math.Max(businessPriority, AnalyzePriorityOffline(title, description));
        }

        public async Task<string> GenerateResponseSuggestionAsync(string ticketContent, string customerMessage, bool isInternal = false)
        {
            if (_apiKey == "demo-key")
            {
                return GenerateResponseOffline(ticketContent, customerMessage, isInternal);
            }

            var responseType = isInternal ? "internal team note" : "customer response";
            var tone = isInternal ? "technical and direct" : "professional, empathetic, and customer-friendly";

            var prompt = $@"Generate a professional {responseType} for this support ticket:

Original Ticket: {ticketContent}
Customer's Latest Message: {customerMessage}

Guidelines:
- Use a {tone} tone
- Address the customer's specific concerns
- Provide actionable next steps
- {(isInternal ? "Include technical details and internal procedures" : "Keep technical language simple and accessible")}
- {(isInternal ? "Suggest troubleshooting steps for the agent" : "Show empathy and understanding")}
- Keep it concise but complete

Generate the {responseType}:";

            var response = await CallOpenAIAsync(prompt);
            return response ?? GenerateResponseOffline(ticketContent, customerMessage, isInternal);
        }

        public async Task<double> AnalyzeSentimentAsync(string text)
        {
            if (_apiKey == "demo-key")
            {
                return AnalyzeSentimentOffline(text);
            }

            var prompt = $@"Analyze the sentiment and emotional tone of this customer message:

Message: {text}

Consider:
- Emotional words (frustrated, angry, pleased, grateful, worried)
- Urgency indicators (ASAP, immediately, urgent, critical)
- Politeness level and tone
- Satisfaction indicators (happy, satisfied, disappointed, upset)
- Escalation language (demand, require, unacceptable)

Return a sentiment score between 0.0 and 1.0 where:
- 0.0-0.2 = Very negative (angry, furious, threatening)
- 0.2-0.4 = Negative (frustrated, disappointed, unhappy)
- 0.4-0.6 = Neutral (business-like, factual, no strong emotion)
- 0.6-0.8 = Positive (satisfied, pleased, grateful)
- 0.8-1.0 = Very positive (delighted, enthusiastic, grateful)

Return only the decimal number, nothing else.";

            var response = await CallOpenAIAsync(prompt);
            
            if (double.TryParse(response?.Trim(), out double sentiment) && sentiment >= 0.0 && sentiment <= 1.0)
            {
                return sentiment;
            }
            
            return AnalyzeSentimentOffline(text);
        }

        public async Task<int> SuggestBestAgentAsync(string ticketContent, IEnumerable<User> availableAgents)
        {
            if (!availableAgents.Any())
                return 0;

            if (_apiKey == "demo-key")
            {
                return SuggestAgentOffline(ticketContent, availableAgents);
            }

            var agentInfo = availableAgents.Select((agent, index) => 
                $"Agent {index + 1}: {agent.FullName} - Skills: {string.Join(", ", agent.AgentSkills.Select(s => s.Skill.Name))} - Current workload: {agent.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress)} tickets");

            var prompt = $@"Given this support ticket, which agent would be best suited to handle it?

Ticket Content: {ticketContent}

Available Agents:
{string.Join("\n", agentInfo)}

Consider:
- Skill matching (technical skills vs ticket requirements)
- Current workload (prefer less busy agents)
- Expertise level for the specific problem type

Return only the agent number (1, 2, 3, etc.), nothing else.";

            var response = await CallOpenAIAsync(prompt);
            
            if (int.TryParse(response?.Trim(), out int agentIndex) && agentIndex >= 1 && agentIndex <= availableAgents.Count())
            {
                return availableAgents.ElementAt(agentIndex - 1).Id;
            }
            
            return SuggestAgentOffline(ticketContent, availableAgents);
        }

        public async Task<AIInsight> CreateInsightAsync(int ticketId, string insightType, object data, double confidence)
        {
            var insightTypeEnum = Enum.Parse<InsightType>(insightType, true);
            
            return new AIInsight
            {
                TicketId = ticketId,
                InsightType = insightTypeEnum,
                Confidence = confidence,
                Data = JsonSerializer.Serialize(data),
                CreatedAt = DateTime.UtcNow
            };
        }

        // Business impact analysis
        private int AnalyzeBusinessImpact(BusinessImpact? businessImpact)
        {
            if (businessImpact == null)
                return 2; // Default medium priority

            var priority = 1; // Start with low

            // Analyze blocking level
            priority = businessImpact.BlockingLevel switch
            {
                BlockingLevel.SystemDown => 4,
                BlockingLevel.CompletelyBlocking => Math.Max(priority, 3),
                BlockingLevel.PartiallyBlocking => Math.Max(priority, 2),
                BlockingLevel.NotBlocking => priority,
                _ => priority
            };

            // Analyze impact scope
            priority = businessImpact.ImpactScope switch
            {
                ImpactScope.Company => Math.Max(priority, 4),
                ImpactScope.Department => Math.Max(priority, 3),
                ImpactScope.Team => Math.Max(priority, 2),
                ImpactScope.Individual => priority,
                _ => priority
            };

            // Check urgent deadline
            if (businessImpact.UrgentDeadline.HasValue && businessImpact.UrgentDeadline.Value <= DateTime.UtcNow.AddHours(4))
            {
                priority = Math.Max(priority, 3); // High priority if deadline within 4 hours
            }

            return Math.Min(priority, 4); // Cap at critical
        }

        // Offline fallback methods for when OpenAI is not available
        private string AnalyzeCategoryOffline(string title, string description)
        {
            var content = (title + " " + description).ToLower();
            
            if (content.Contains("login") || content.Contains("password") || content.Contains("access") || content.Contains("account"))
                return "Account Problem";
            
            if (content.Contains("bill") || content.Contains("payment") || content.Contains("invoice") || content.Contains("charge"))
                return "Billing Question";
            
            if (content.Contains("crash") || content.Contains("error") || content.Contains("bug") || content.Contains("broken"))
                return "Bug Report";
            
            if (content.Contains("feature") || content.Contains("enhancement") || content.Contains("request") || content.Contains("improve"))
                return "Feature Request";
            
            if (content.Contains("technical") || content.Contains("system") || content.Contains("server") || content.Contains("database"))
                return "Technical Issue";
            
            return "General Inquiry";
        }

        private int AnalyzePriorityOffline(string title, string description)
        {
            var content = (title + " " + description).ToLower();
            
            // Critical indicators
            if (content.Contains("down") || content.Contains("critical") || content.Contains("system failure") || 
                content.Contains("can't work") || content.Contains("production") || content.Contains("all users"))
                return 4;
            
            // High priority indicators
            if (content.Contains("urgent") || content.Contains("asap") || content.Contains("blocking") || 
                content.Contains("frustrated") || content.Contains("angry") || content.Contains("!!!"))
                return 3;
            
            // Low priority indicators
            if (content.Contains("when possible") || content.Contains("nice to have") || 
                content.Contains("suggestion") || content.Contains("minor"))
                return 1;
            
            return 2; // Default medium
        }

        private double AnalyzeSentimentOffline(string text)
        {
            var content = text.ToLower();
            double score = 0.5; // Start neutral
            
            // Negative indicators
            if (content.Contains("angry") || content.Contains("furious")) score -= 0.3;
            if (content.Contains("frustrated") || content.Contains("annoyed")) score -= 0.2;
            if (content.Contains("disappointed") || content.Contains("upset")) score -= 0.15;
            if (content.Contains("terrible") || content.Contains("awful")) score -= 0.2;
            if (content.Contains("unacceptable") || content.Contains("ridiculous")) score -= 0.25;
            
            // Positive indicators
            if (content.Contains("thank") || content.Contains("appreciate")) score += 0.2;
            if (content.Contains("great") || content.Contains("excellent")) score += 0.2;
            if (content.Contains("pleased") || content.Contains("satisfied")) score += 0.15;
            if (content.Contains("happy") || content.Contains("glad")) score += 0.1;
            
            // Urgency affects sentiment negatively
            if (content.Contains("urgent") || content.Contains("asap")) score -= 0.1;
            if (content.Contains("immediately") || content.Contains("critical")) score -= 0.15;
            
            return Math.Max(0.0, Math.Min(1.0, score));
        }

        private string GenerateResponseOffline(string ticketContent, string customerMessage, bool isInternal)
        {
            if (isInternal)
            {
                return "Internal note: This ticket requires further investigation. Check system logs and user account status. Consider escalating if the issue persists after basic troubleshooting.";
            }

            return "Thank you for contacting us. I understand your concern and I'm here to help resolve this issue for you. Let me look into this right away and get back to you with a solution. I'll update you within the next hour on our progress.";
        }

        private int SuggestAgentOffline(string ticketContent, IEnumerable<User> availableAgents)
        {
            // Simple fallback: assign to agent with lowest workload
            return availableAgents
                .OrderBy(a => a.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress))
                .First()
                .Id;
        }

        private async Task<string?> CallOpenAIAsync(string prompt)
        {
            if (_apiKey == "demo-key")
            {
                // Simulate API delay
                await Task.Delay(1000);
                return null;
            }

            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful AI assistant for customer support ticket analysis. Provide concise, accurate responses." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 150,
                    temperature = 0.1
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    
                    return responseObj
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                }
                
                return null;
            }
            catch (Exception)
            {
                // Log the exception in production
                return null;
            }
        }
    }
}
