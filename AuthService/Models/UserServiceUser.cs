namespace AuthService.Models;

/// <summary>
/// Пользователь, полученный из user-service.
/// </summary>
public class UserServiceUser
{
    public Guid Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
}
