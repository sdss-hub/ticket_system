using Microsoft.AspNetCore.Mvc;
using SupportTicketSystem.API.DTOs;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Interfaces;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoriesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetAllAsync();
                var categoryDtos = categories.Select(MapToCategoryDto).ToList();
                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving categories", error = ex.Message });
            }
        }

        [HttpGet("hierarchy")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategoryHierarchy()
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetCategoryHierarchyAsync();
                var categoryDtos = categories.Select(MapToCategoryDto).ToList();
                return Ok(categoryDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving category hierarchy", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetCategoryWithSubCategoriesAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                var categoryDto = MapToCategoryDto(category);
                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the category", error = ex.Message });
            }
        }

        private static CategoryDto MapToCategoryDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                Level = category.Level,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                ParentCategory = category.ParentCategory != null ? new CategoryDto
                {
                    Id = category.ParentCategory.Id,
                    Name = category.ParentCategory.Name
                } : null,
                SubCategories = category.SubCategories?.Select(sc => new CategoryDto
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    Level = sc.Level
                }).ToList() ?? new List<CategoryDto>()
            };
        }
    }
}
