using Microsoft.AspNetCore.Mvc;
using SupportTicketSystem.API.DTOs;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserRole? role = null)
        {
            try
            {
                IEnumerable<User> users;
                
                if (role.HasValue)
                {
                    users = await _unitOfWork.Users.GetUsersByRoleAsync(role.Value);
                }
                else
                {
                    users = await _unitOfWork.Users.GetAllAsync();
                }

                var userDtos = users.Select(MapToUserDto).ToList();
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = MapToUserDto(user);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the user", error = ex.Message });
            }
        }

        [HttpGet("agents/available")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAvailableAgents()
        {
            try
            {
                var agents = await _unitOfWork.Users.GetAvailableAgentsAsync();
                var agentDtos = agents.Select(MapToUserDto).ToList();
                return Ok(agentDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving available agents", error = ex.Message });
            }
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
