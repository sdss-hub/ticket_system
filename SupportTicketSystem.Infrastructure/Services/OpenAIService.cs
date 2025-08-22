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
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is missing");
            _model = configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> CategorizeSupportTicketAsync(string title, string description)
        {
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
            return response?.Trim() ?? "General Inquiry";
        }

        public async Task<int> AnalyzePriorityAsync(string title, string description)
        {
            var prompt = $@"Analyze this support ticket and determine its priority level:
1 = Low (general questions, minor issues)
2 = Medium (moderate impact issues)  
3 = High (significant impact, urgent)
4 = Critical (system down, major blocker)

Title: {title}
Description: {description}

Return only the priority number (1-4), nothing else.";

            var response = await CallOpenAIAsync(prompt);
            
            if (int.TryParse(response?.Trim(), out int priority) && priority >= 1 && priority <= 4)
            {
                return priority;
            }
            
            return 2; // Default to Medium priority
        }

        public async Task<string> GenerateResponseSuggestionAsync(string ticketContent, string customerMessage)
        {
            var prompt = $@"Generate a professional customer support response for this ticket:

Original Ticket: {ticketContent}
Customer Message: {customerMessage}

Generate a helpful, empathetic response that addresses the customer's concern. Keep it professional and concise.";

            var response = await CallOpenAIAsync(prompt);
            return response ?? "Thank you for contacting us. We'll look into this issue and get back to you soon.";
        }

        public async Task<double> AnalyzeSentimentAsync(string text)
        {
            var prompt = $@"Analyze the sentiment of this customer message and return a sentiment score:
- Return a number between 0.0 (very negative) and 1.0 (very positive)
- 0.5 is neutral
- Consider urgency, frustration level, politeness

Message: {text}

Return only the decimal number, nothing else.";

            var response = await CallOpenAIAsync(prompt);
            
            if (double.TryParse(response?.Trim(), out double sentiment) && sentiment >= 0.0 && sentiment <= 1.0)
            {
                return sentiment;
            }
            
            return 0.5; // Default to neutral sentiment
        }

        public async Task<int> SuggestBestAgentAsync(string ticketContent, IEnumerable<User> availableAgents)
        {
            if (!availableAgents.Any())
                return 0;

            var agentInfo = availableAgents.Select((agent, index) => 
                $"Agent {index + 1}: {agent.FullName} - Skills: {string.Join(", ", agent.AgentSkills.Select(s => s.Skill.Name))}");

            var prompt = $@"Given this support ticket, which agent would be best suited to handle it?

Ticket Content: {ticketContent}

Available Agents:
{string.Join("\n", agentInfo)}

Return only the agent number (1, 2, 3, etc.), nothing else.";

            var response = await CallOpenAIAsync(prompt);
            
            if (int.TryParse(response?.Trim(), out int agentIndex) && agentIndex >= 1 && agentIndex <= availableAgents.Count())
            {
                return availableAgents.ElementAt(agentIndex - 1).Id;
            }
            
            return availableAgents.First().Id; // Default to first available agent
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

        private async Task<string?> CallOpenAIAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful AI assistant for customer support ticket analysis." },
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
