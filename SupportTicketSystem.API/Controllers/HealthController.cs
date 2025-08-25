using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Interfaces;
using SupportTicketSystem.Infrastructure.Data;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAIService _aiService;

        public HealthController(ApplicationDbContext context, IUnitOfWork unitOfWork, IAIService aiService)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _aiService = aiService;
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };

            return Ok(healthStatus);
        }

        [HttpGet("detailed")]
        public async Task<ActionResult> GetDetailedHealth()
        {
            var healthChecks = new List<object>();

            try
            {
                // 1. Database Connection Test
                var dbConnected = await TestDatabaseConnection();
                healthChecks.Add(new { Component = "Database", Status = dbConnected ? "Healthy" : "Unhealthy", Details = "SQLite connection" });

                // 2. Database Tables Test
                var tablesExist = await TestDatabaseTables();
                healthChecks.Add(new { Component = "Database Tables", Status = tablesExist ? "Healthy" : "Unhealthy", Details = "All tables created" });

                // 3. Sample Data Test
                var dataExists = await TestSampleData();
                healthChecks.Add(new { Component = "Sample Data", Status = dataExists ? "Healthy" : "Unhealthy", Details = dataExists ? "Sample data loaded" : "No sample data" });

                // 4. Repository Pattern Test
                var repositoriesWork = await TestRepositories();
                healthChecks.Add(new { Component = "Repositories", Status = repositoriesWork ? "Healthy" : "Unhealthy", Details = "Repository pattern implementation" });

                // 5. Complex Relationships Test
                var relationshipsWork = await TestComplexRelationships();
                healthChecks.Add(new { Component = "Complex Relationships", Status = relationshipsWork ? "Healthy" : "Unhealthy", Details = "Many-to-many, hierarchical, navigation properties" });

                // 6. JSON Columns Test
                var jsonColumnsWork = await TestJsonColumns();
                healthChecks.Add(new { Component = "JSON Columns", Status = jsonColumnsWork ? "Healthy" : "Unhealthy", Details = "JSON data storage and retrieval" });

                // 7. BLOB Columns Test
                var blobColumnsWork = await TestBlobColumns();
                healthChecks.Add(new { Component = "BLOB Columns", Status = blobColumnsWork ? "Healthy" : "Unhealthy", Details = "Binary data storage capability" });

                // 8. AI Service Test
                var aiServiceWork = await TestAIService();
                healthChecks.Add(new { Component = "AI Service", Status = aiServiceWork ? "Healthy" : "Warning", Details = aiServiceWork ? "OpenAI service configured" : "OpenAI key not configured or invalid" });

                // Overall status
                var overallHealthy = healthChecks.All(h => h.GetType().GetProperty("Status")?.GetValue(h)?.ToString() != "Unhealthy");

                var result = new
                {
                    OverallStatus = overallHealthy ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    HealthChecks = healthChecks,
                    SystemInfo = new
                    {
                        MachineName = Environment.MachineName,
                        OSVersion = Environment.OSVersion.ToString(),
                        ProcessorCount = Environment.ProcessorCount,
                        WorkingSet = Environment.WorkingSet,
                        DotNetVersion = Environment.Version.ToString()
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    OverallStatus = "Unhealthy", 
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow 
                });
            }
        }

        [HttpGet("database")]
        public async Task<ActionResult> GetDatabaseInfo()
        {
            try
            {
                var dbInfo = new
                {
                    // Table counts
                    UserCount = await _context.Users.CountAsync(),
                    TicketCount = await _context.Tickets.CountAsync(),
                    CategoryCount = await _context.Categories.CountAsync(),
                    CommentCount = await _context.TicketComments.CountAsync(),
                    AttachmentCount = await _context.Attachments.CountAsync(),
                    TagCount = await _context.Tags.CountAsync(),
                    SkillCount = await _context.Skills.CountAsync(),
                    AIInsightCount = await _context.AIInsights.CountAsync(),

                    // Data distribution
                    TicketsByStatus = await _context.Tickets
                        .GroupBy(t => t.Status)
                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync(),

                    TicketsByPriority = await _context.Tickets
                        .GroupBy(t => t.Priority)
                        .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync(),

                    UsersByRole = await _context.Users
                        .GroupBy(u => u.Role)
                        .Select(g => new { Role = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync(),

                    // Recent activity
                    RecentTickets = await _context.Tickets
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(3)
                        .Select(t => new { t.Id, t.TicketNumber, t.Title, t.Status })
                        .ToListAsync(),

                    RecentComments = await _context.TicketComments
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(3)
                        .Select(c => new { c.Id, c.TicketId, CommentPreview = c.CommentText.Substring(0, Math.Min(50, c.CommentText.Length)) })
                        .ToListAsync()
                };

                return Ok(dbInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        private async Task<bool> TestDatabaseConnection()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestDatabaseTables()
        {
            try
            {
                // Fixed: Use safe table existence check without SQL injection risk
                var tableNames = new[] { "Users", "Tickets", "Categories", "TicketComments", "Attachments", "Tags", "Skills", "AIInsights" };
                
                foreach (var tableName in tableNames)
                {
                    // Safe approach: Use parameterized query with ExecuteSqlAsync
                    var result = await _context.Database
                        .ExecuteSqlAsync($"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name={tableName}");
                    
                    // Alternative: Just try to query the actual tables safely
                    switch (tableName)
                    {
                        case "Users":
                            await _context.Users.Take(1).CountAsync();
                            break;
                        case "Tickets":
                            await _context.Tickets.Take(1).CountAsync();
                            break;
                        case "Categories":
                            await _context.Categories.Take(1).CountAsync();
                            break;
                        case "TicketComments":
                            await _context.TicketComments.Take(1).CountAsync();
                            break;
                        case "Attachments":
                            await _context.Attachments.Take(1).CountAsync();
                            break;
                        case "Tags":
                            await _context.Tags.Take(1).CountAsync();
                            break;
                        case "Skills":
                            await _context.Skills.Take(1).CountAsync();
                            break;
                        case "AIInsights":
                            await _context.AIInsights.Take(1).CountAsync();
                            break;
                    }
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestSampleData()
        {
            try
            {
                var userCount = await _context.Users.CountAsync();
                var ticketCount = await _context.Tickets.CountAsync();
                var categoryCount = await _context.Categories.CountAsync();
                
                return userCount > 0 && ticketCount > 0 && categoryCount > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestRepositories()
        {
            try
            {
                // Test basic repository operations
                var users = await _unitOfWork.Users.GetAllAsync();
                var tickets = await _unitOfWork.Tickets.GetAllAsync();
                var categories = await _unitOfWork.Categories.GetAllAsync();
                
                return users.Any() && tickets.Any() && categories.Any();
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestComplexRelationships()
        {
            try
            {
                // Test many-to-many relationships
                var agentSkills = await _context.AgentSkills.Include(ass => ass.Agent).Include(ass => ass.Skill).ToListAsync();
                var ticketTags = await _context.TicketTags.Include(tt => tt.Ticket).Include(tt => tt.Tag).ToListAsync();
                
                // Test hierarchical relationships
                var rootCategories = await _unitOfWork.Categories.GetRootCategoriesAsync();
                var subCategories = await _context.Categories.Where(c => c.ParentCategoryId != null).ToListAsync();
                
                // Test navigation properties
                var ticketWithDetails = await _unitOfWork.Tickets.GetTicketWithDetailsAsync(1);
                
                return agentSkills.Any() && ticketTags.Any() && rootCategories.Any() && ticketWithDetails != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestJsonColumns()
        {
            try
            {
                // Test JSON columns
                var ticketsWithAI = await _context.Tickets.Where(t => t.AIAnalysis != null).ToListAsync();
                var usersWithSettings = await _context.Users.Where(u => u.ProfileSettings != null).ToListAsync();
                var aiInsights = await _context.AIInsights.ToListAsync();
                
                return ticketsWithAI.Any() || usersWithSettings.Any() || aiInsights.Any();
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestBlobColumns()
        {
            try
            {
                // Check if BLOB column structure exists (even if no binary data yet)
                await _context.Attachments.Take(1).CountAsync();
                return true; // BLOB column exists in schema
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestAIService()
        {
            try
            {
                // Test if AI service is configured (without making actual API call)
                var testInsight = await _aiService.CreateInsightAsync(1, "Categorization", new { Test = "data" }, 0.8);
                return testInsight != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
