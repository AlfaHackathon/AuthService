namespace AuthService.Contracts;

/// <summary>Данные для создания пользователя (уходят в user-service).</summary>
public record CreateUserRequest(string LastName, string FirstName, string? MiddleName, string Email, string HashedPassword);
