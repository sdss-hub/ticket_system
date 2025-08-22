using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Infrastructure.Data;
using SupportTicketSystem.API.DTOs;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdvancedSearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdvancedSearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("tickets")]
        public async Task<ActionResult> SearchTickets([FromBody] TicketSearchDto searchCriteria)
        {
            try
            {
                var query = _context.Tickets
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedAgent)
                    .Include(t => t.Category)
                    .Include(t => t.TicketTags).ThenInclude(tt => tt.Tag)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchCriteria.Keywords))
                {
                    query = query.Where(t => 
                        t.Title.Contains(searchCriteria.Keywords) ||
                        t.Description.Contains(searchCriteria.Keywords) ||
                        t.TicketNumber.Contains(searchCriteria.Keywords));
                }

                if (searchCriteria.Status.HasValue)
                    query = query.Where(t => t.Status == searchCriteria.Status.Value);

                if (searchCriteria.Priority.HasValue)
                    query = query.Where(t => t.Priority == searchCriteria.Priority.Value);

                if (searchCriteria.CategoryId.HasValue)
                    query = query.Where(t => t.CategoryId == searchCriteria.CategoryId.Value);

                if (searchCriteria.AssignedAgentId.HasValue)
                    query = query.Where(t => t.AssignedAgentId == searchCriteria.AssignedAgentId.Value);

                if (searchCriteria.CustomerId.HasValue)
                    query = query.Where(t => t.CustomerId == searchCriteria.CustomerId.Value);

                if (searchCriteria.CreatedAfter.HasValue)
                    query = query.Where(t => t.CreatedAt >= searchCriteria.CreatedAfter.Value);

                if (searchCriteria.CreatedBefore.HasValue)
                    query = query.Where(t => t.CreatedAt <= searchCriteria.CreatedBefore.Value);

                if (searchCriteria.HasAttachments.HasValue)
                {
                    if (searchCriteria.HasAttachments.Value)
                        query = query.Where(t => t.Attachments.Any());
                    else
                        query = query.Where(t => !t.Attachments.Any());
                }

                if (searchCriteria.Tags != null && searchCriteria.Tags.Any())
                {
                    query = query.Where(t => t.TicketTags.Any(tt => searchCriteria.Tags.Contains(tt.Tag.Name)));
                }

                // Apply sorting
                query = searchCriteria.SortBy?.ToLower() switch
                {
                    "created" => searchCriteria.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                    "updated" => searchCriteria.SortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
                    "priority" => searchCriteria.SortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
                    "status" => searchCriteria.SortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
                    "title" => searchCriteria.SortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
                    _ => query.OrderByDescending(t => t.CreatedAt)
                };

                // Pagination
                var totalCount = await query.CountAsync();
                var pageSize = Math.Min(searchCriteria.PageSize ?? 50, 100); // Max 100 per page
                var pageNumber = Math.Max(searchCriteria.PageNumber ?? 1, 1);
                var skip = (pageNumber - 1) * pageSize;

                var tickets = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.TicketNumber,
                        t.Title,
                        Description = t.Description.Length > 200 ? t.Description.Substring(0, 200) + "..." : t.Description,
                        t.Priority,
                        t.Status,
                        t.CreatedAt,
                        t.UpdatedAt,
                        t.DueDate,
                        Customer = new { t.Customer.Id, t.Customer.FullName, t.Customer.Email },
                        AssignedAgent = t.AssignedAgent != null ? new { t.AssignedAgent.Id, t.AssignedAgent.FullName } : null,
                        Category = t.Category != null ? new { t.Category.Id, t.Category.Name } : null,
                        Tags = t.TicketTags.Select(tt => new { tt.Tag.Id, tt.Tag.Name, tt.Tag.Color }).ToList(),
                        AttachmentCount = t.Attachments.Count(),
                        CommentCount = t.Comments.Count()
                    })
                    .ToListAsync();

                var result = new
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    Tickets = tickets
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Search failed", error = ex.Message });
            }
        }

        [HttpGet("suggestions")]
        public async Task<ActionResult> GetSearchSuggestions([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrEmpty(term) || term.Length < 2)
                    return Ok(new { suggestions = new object[0] });

                var suggestions = new
                {
                    TicketNumbers = await _context.Tickets
                        .Where(t => t.TicketNumber.Contains(term))
                        .Take(5)
                        .Select(t => new { t.TicketNumber, t.Title })
                        .ToListAsync(),

                    CustomerNames = await _context.Users
                        .Where(u => u.Role == UserRole.Customer && 
                                   (u.FirstName.Contains(term) || u.LastName.Contains(term) || u.Email.Contains(term)))
                        .Take(5)
                        .Select(u => new { u.Id, u.FullName, u.Email })
                        .ToListAsync(),

                    Categories = await _context.Categories
                        .Where(c => c.Name.Contains(term) && c.IsActive)
                        .Take(5)
                        .Select(c => new { c.Id, c.Name })
                        .ToListAsync(),

                    Tags = await _context.Tags
                        .Where(t => t.Name.Contains(term))
                        .Take(5)
                        .Select(t => new { t.Id, t.Name, t.Color })
                        .ToListAsync()
                };

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get suggestions", error = ex.Message });
            }
        }
    }
}

public class TicketSearchDto
{
    public string? Keywords { get; set; }
    public TicketStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public int? CategoryId { get; set; }
    public int? AssignedAgentId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public bool? HasAttachments { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; } = "created";
    public bool SortDescending { get; set; } = true;
    public int? PageNumber { get; set; } = 1;
    public int? PageSize { get; set; } = 20;
}
