using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Core.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> RegisterAsync(string email, string password, string firstName, string lastName, Core.Enums.UserRole role = Core.Enums.UserRole.Customer);
        Task<string> GenerateJwtTokenAsync(User user);
        Task<bool> ValidateTokenAsync(string token);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}
