using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using System.Text.Json;

namespace SupportTicketSystem.Infrastructure.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Check if data already exists
            if (await context.Users.AnyAsync())
            {
                return; // Database already seeded
            }

            await SeedUsersAsync(context);
            await SeedCategoriesAsync(context);
            await SeedSkillsAndTagsAsync(context);
            await SeedTicketsAsync(context);
            
            await context.SaveChangesAsync();
        }

        private static async Task SeedUsersAsync(ApplicationDbContext context)
        {
            var users = new List<User>
            {
                // Customers
                new User
                {
                    Email = "john.doe@customer.com",
                    PasswordHash = "hashed_password_123", // In real app, properly hash this
                    FirstName = "John",
                    LastName = "Doe",
                    Role = UserRole.Customer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    ProfileSettings = JsonSerializer.Serialize(new { Theme = "light", Notifications = true })
                },
                new User
                {
                    Email = "jane.smith@customer.com",
                    PasswordHash = "hashed_password_123",
                    FirstName = "Jane",
                    LastName = "Smith", 
                    Role = UserRole.Customer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    ProfileSettings = JsonSerializer.Serialize(new { Theme = "dark", Notifications = false })
                },
                new User
                {
                    Email = "mike.wilson@customer.com",
                    PasswordHash = "hashed_password_123",
                    FirstName = "Mike",
                    LastName = "Wilson",
                    Role = UserRole.Customer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },

                // Agents
                new User
                {
                    Email = "sarah.johnson@support.com",
                    PasswordHash = "hashed_password_123",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Role = UserRole.Agent,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    LastLoginAt = DateTime.UtcNow.AddHours(-2)
                },
                new User
                {
                    Email = "david.brown@support.com",
                    PasswordHash = "hashed_password_123",
                    FirstName = "David",
                    LastName = "Brown",
                    Role = UserRole.Agent,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-55),
                    LastLoginAt = DateTime.UtcNow.AddHours(-1)
                },
                new User
                {
                    Email = "lisa.garcia@support.com",
                    PasswordHash = "hashed_password_123",
                    FirstName = "Lisa",
                    LastName = "Garcia",
                    Role = UserRole.Agent,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    LastLoginAt = DateTime.UtcNow.AddMinutes(-30)
                },

                // Admin
                new User
                {
                    Email = "admin@supporttickets.com",
                    PasswordHash = "hashed_password_123",
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    LastLoginAt = DateTime.UtcNow.AddMinutes(-10),
                    ProfileSettings = JsonSerializer.Serialize(new { Theme = "dark", AdminMode = true })
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            var categories = new List<Category>
            {
                // Root categories
                new Category
                {
                    Name = "Technical Issue",
                    Description = "Software bugs, system errors, performance issues",
                    Level = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70)
                },
                new Category
                {
                    Name = "Account Problem",
                    Description = "Login issues, account access, password resets",
                    Level = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70)
                },
                new Category
                {
                    Name = "Billing Question",
                    Description = "Payment issues, invoices, subscription questions",
                    Level = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70)
                },
                new Category
                {
                    Name = "Feature Request",
                    Description = "New feature suggestions and enhancements",
                    Level = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70)
                },
                new Category
                {
                    Name = "General Inquiry",
                    Description = "General questions and information requests",
                    Level = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70)
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // Add subcategories
            var technicalCategory = await context.Categories.FirstAsync(c => c.Name == "Technical Issue");
            var accountCategory = await context.Categories.FirstAsync(c => c.Name == "Account Problem");

            var subCategories = new List<Category>
            {
                new Category
                {
                    Name = "Bug Report",
                    Description = "Software bugs and defects",
                    ParentCategoryId = technicalCategory.Id,
                    Level = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-65)
                },
                new Category
                {
                    Name = "Performance Issue",
                    Description = "Slow loading, timeouts, system performance",
                    ParentCategoryId = technicalCategory.Id,
                    Level = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-65)
                },
                new Category
                {
                    Name = "Login Problem",
                    Description = "Cannot login, authentication issues",
                    ParentCategoryId = accountCategory.Id,
                    Level = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-65)
                },
                new Category
                {
                    Name = "Password Reset",
                    Description = "Forgotten password, reset requests",
                    ParentCategoryId = accountCategory.Id,
                    Level = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-65)
                }
            };

            await context.Categories.AddRangeAsync(subCategories);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSkillsAndTagsAsync(ApplicationDbContext context)
        {
            // Skills
            var skills = new List<Skill>
            {
                new Skill { Name = "JavaScript", Description = "Frontend and backend JavaScript development" },
                new Skill { Name = "Python", Description = "Python programming and scripting" },
                new Skill { Name = "Database", Description = "SQL and database management" },
                new Skill { Name = "Networking", Description = "Network configuration and troubleshooting" },
                new Skill { Name = "Security", Description = "Cybersecurity and data protection" },
                new Skill { Name = "Mobile Apps", Description = "iOS and Android application support" },
                new Skill { Name = "Cloud Services", Description = "AWS, Azure, Google Cloud platforms" }
            };

            await context.Skills.AddRangeAsync(skills);
            await context.SaveChangesAsync();

            // Assign skills to agents
            var agents = await context.Users.Where(u => u.Role == UserRole.Agent).ToListAsync();
            var skillsList = await context.Skills.ToListAsync();

            var agentSkills = new List<AgentSkill>
            {
                // Sarah Johnson - JavaScript, Database
                new AgentSkill { AgentId = agents[0].Id, SkillId = skillsList[0].Id, ProficiencyLevel = 5 },
                new AgentSkill { AgentId = agents[0].Id, SkillId = skillsList[2].Id, ProficiencyLevel = 4 },
                
                // David Brown - Python, Cloud Services
                new AgentSkill { AgentId = agents[1].Id, SkillId = skillsList[1].Id, ProficiencyLevel = 5 },
                new AgentSkill { AgentId = agents[1].Id, SkillId = skillsList[6].Id, ProficiencyLevel = 4 },
                
                // Lisa Garcia - Security, Networking
                new AgentSkill { AgentId = agents[2].Id, SkillId = skillsList[4].Id, ProficiencyLevel = 5 },
                new AgentSkill { AgentId = agents[2].Id, SkillId = skillsList[3].Id, ProficiencyLevel = 4 }
            };

            await context.AgentSkills.AddRangeAsync(agentSkills);

            // Tags
            var tags = new List<Tag>
            {
                new Tag { Name = "Urgent", Color = "#dc3545" },
                new Tag { Name = "Bug", Color = "#fd7e14" },
                new Tag { Name = "Enhancement", Color = "#28a745" },
                new Tag { Name = "Question", Color = "#17a2b8" },
                new Tag { Name = "Documentation", Color = "#6f42c1" },
                new Tag { Name = "Mobile", Color = "#e83e8c" },
                new Tag { Name = "API", Color = "#20c997" }
            };

            await context.Tags.AddRangeAsync(tags);
            await context.SaveChangesAsync();
        }

        private static async Task SeedTicketsAsync(ApplicationDbContext context)
        {
            var customers = await context.Users.Where(u => u.Role == UserRole.Customer).ToListAsync();
            var agents = await context.Users.Where(u => u.Role == UserRole.Agent).ToListAsync();
            var categories = await context.Categories.Where(c => c.Level == 0).ToListAsync();
            var tags = await context.Tags.ToListAsync();

            var tickets = new List<Ticket>
            {
                new Ticket
                {
                    TicketNumber = DateTime.UtcNow.ToString("yyyyMMdd") + "0001",
                    CustomerId = customers[0].Id,
                    AssignedAgentId = agents[0].Id,
                    CategoryId = categories.First(c => c.Name == "Technical Issue").Id,
                    Title = "Application crashes when uploading large files",
                    Description = "When I try to upload files larger than 10MB, the application crashes and I lose all my work. This happens consistently across different file types (PDF, images, documents). Please help resolve this issue as it's affecting my productivity.",
                    Priority = Priority.High,
                    Status = TicketStatus.InProgress,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    DueDate = DateTime.UtcNow.AddDays(2),
                    AIAnalysis = JsonSerializer.Serialize(new
                    {
                        SuggestedCategory = "Technical Issue",
                        SuggestedPriority = 3,
                        SentimentScore = 0.3,
                        AnalyzedAt = DateTime.UtcNow.AddDays(-5)
                    })
                },
                new Ticket
                {
                    TicketNumber = DateTime.UtcNow.ToString("yyyyMMdd") + "0002",
                    CustomerId = customers[1].Id,
                    CategoryId = categories.First(c => c.Name == "Account Problem").Id,
                    Title = "Cannot reset my password",
                    Description = "I've tried multiple times to reset my password using the 'Forgot Password' link, but I'm not receiving any reset emails. I've checked my spam folder and tried different browsers. My account email is jane.smith@customer.com.",
                    Priority = Priority.Medium,
                    Status = TicketStatus.New,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3),
                    DueDate = DateTime.UtcNow.AddDays(5),
                    AIAnalysis = JsonSerializer.Serialize(new
                    {
                        SuggestedCategory = "Account Problem",
                        SuggestedPriority = 2,
                        SentimentScore = 0.4,
                        AnalyzedAt = DateTime.UtcNow.AddDays(-3)
                    })
                },
                new Ticket
                {
                    TicketNumber = DateTime.UtcNow.ToString("yyyyMMdd") + "0003",
                    CustomerId = customers[2].Id,
                    AssignedAgentId = agents[1].Id,
                    CategoryId = categories.First(c => c.Name == "Feature Request").Id,
                    Title = "Add dark mode theme option",
                    Description = "It would be great if the application had a dark mode theme option. Many users prefer dark interfaces, especially when working in low-light environments. This would improve user experience and reduce eye strain during extended usage.",
                    Priority = Priority.Low,
                    Status = TicketStatus.Resolved,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    ResolvedAt = DateTime.UtcNow.AddDays(-1),
                    AIAnalysis = JsonSerializer.Serialize(new
                    {
                        SuggestedCategory = "Feature Request",
                        SuggestedPriority = 1,
                        SentimentScore = 0.8,
                        AnalyzedAt = DateTime.UtcNow.AddDays(-10)
                    })
                },
                new Ticket
                {
                    TicketNumber = DateTime.UtcNow.ToString("yyyyMMdd") + "0004",
                    CustomerId = customers[0].Id,
                    AssignedAgentId = agents[2].Id,
                    CategoryId = categories.First(c => c.Name == "Billing Question").Id,
                    Title = "Incorrect charges on my invoice",
                    Description = "I received my monthly invoice and there are charges that I don't recognize. There's a $50 fee for 'Premium Support' that I never signed up for. Can someone please review my account and correct this billing error?",
                    Priority = Priority.High,
                    Status = TicketStatus.InProgress,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(3),
                    AIAnalysis = JsonSerializer.Serialize(new
                    {
                        SuggestedCategory = "Billing Question",
                        SuggestedPriority = 3,
                        SentimentScore = 0.2,
                        AnalyzedAt = DateTime.UtcNow.AddDays(-2)
                    })
                },
                new Ticket
                {
                    TicketNumber = DateTime.UtcNow.ToString("yyyyMMdd") + "0005",
                    CustomerId = customers[1].Id,
                    CategoryId = categories.First(c => c.Name == "General Inquiry").Id,
                    Title = "How to export my data?",
                    Description = "I need to export all my data from the platform for backup purposes. Is there a way to download everything at once, or do I need to export each section separately? Please provide step-by-step instructions.",
                    Priority = Priority.Low,
                    Status = TicketStatus.New,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(7),
                    AIAnalysis = JsonSerializer.Serialize(new
                    {
                        SuggestedCategory = "General Inquiry",
                        SuggestedPriority = 1,
                        SentimentScore = 0.6,
                        AnalyzedAt = DateTime.UtcNow.AddDays(-1)
                    })
                }
            };

            await context.Tickets.AddRangeAsync(tickets);
            await context.SaveChangesAsync();

            // Add some comments to tickets
            var comments = new List<TicketComment>
            {
                new TicketComment
                {
                    TicketId = tickets[0].Id,
                    UserId = agents[0].Id,
                    CommentText = "I've received your ticket regarding the file upload issue. I'm looking into this now and will test with various file sizes to reproduce the problem.",
                    IsInternal = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new TicketComment
                {
                    TicketId = tickets[0].Id,
                    UserId = agents[0].Id,
                    CommentText = "Internal note: This seems related to the recent server memory limits we implemented. Need to check with DevOps team.",
                    IsInternal = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new TicketComment
                {
                    TicketId = tickets[0].Id,
                    UserId = customers[0].Id,
                    CommentText = "Thank you for looking into this. The issue is still occurring. I've tried with different file types and they all fail around the 10MB mark.",
                    IsInternal = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new TicketComment
                {
                    TicketId = tickets[2].Id,
                    UserId = agents[1].Id,
                    CommentText = "Great suggestion! I've forwarded this to our UI/UX team for consideration in the next release cycle.",
                    IsInternal = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new TicketComment
                {
                    TicketId = tickets[2].Id,
                    UserId = agents[1].Id,
                    CommentText = "Update: Dark mode theme has been implemented and will be available in the next update. Thanks for the feedback!",
                    IsInternal = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            await context.TicketComments.AddRangeAsync(comments);

            // Add some ticket tags
            var ticketTags = new List<TicketTag>
            {
                new TicketTag { TicketId = tickets[0].Id, TagId = tags.First(t => t.Name == "Urgent").Id },
                new TicketTag { TicketId = tickets[0].Id, TagId = tags.First(t => t.Name == "Bug").Id },
                new TicketTag { TicketId = tickets[2].Id, TagId = tags.First(t => t.Name == "Enhancement").Id },
                new TicketTag { TicketId = tickets[3].Id, TagId = tags.First(t => t.Name == "Urgent").Id },
                new TicketTag { TicketId = tickets[4].Id, TagId = tags.First(t => t.Name == "Question").Id }
            };

            await context.TicketTags.AddRangeAsync(ticketTags);

            // Add some AI insights
            var aiInsights = new List<AIInsight>
            {
                new AIInsight
                {
                    TicketId = tickets[0].Id,
                    InsightType = InsightType.Categorization,
                    Confidence = 0.92,
                    Data = JsonSerializer.Serialize(new { Category = "Technical Issue", Reasoning = "File upload crash indicates technical problem" }),
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new AIInsight
                {
                    TicketId = tickets[0].Id,
                    InsightType = InsightType.Sentiment,
                    Confidence = 0.87,
                    Data = JsonSerializer.Serialize(new { Sentiment = "Frustrated", Score = 0.3, Keywords = new[] { "crashes", "lose work", "affecting productivity" } }),
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new AIInsight
                {
                    TicketId = tickets[1].Id,
                    InsightType = InsightType.Priority,
                    Confidence = 0.78,
                    Data = JsonSerializer.Serialize(new { Priority = "Medium", Reasoning = "Account access issue affects user but not critical" }),
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            await context.AIInsights.AddRangeAsync(aiInsights);

            await context.SaveChangesAsync();
        }
    }
}
