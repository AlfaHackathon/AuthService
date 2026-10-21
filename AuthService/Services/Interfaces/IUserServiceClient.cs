namespace AuthService.Services.Interfaces;

/// <summary>Клиент user-service (internal-ручки, ходим по HTTP как сервис кластера).</summary>
public interface IUserServiceClient
{
    /// <summary>Зарегистрирован ли email.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Пользователь по email (с хэшем пароля), null — не найден/недоступен.</summary>
    Task<AuthService.Models.UserServiceUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Создать пользователя. null — ошибка создания (409 / недоступность user-service).</summary>
    Task<AuthService.Models.UserServiceUser?> AddAsync(AuthService.Contracts.CreateUserRequest request, CancellationToken cancellationToken = default);
}
