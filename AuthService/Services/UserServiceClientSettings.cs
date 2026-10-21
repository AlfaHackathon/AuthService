using AlfaHackaton.HttpClient.Common.Settings;

namespace AuthService.Services;

/// <summary>
/// Настройки клиента user-service: BaseAddress — из appsettings (Services:UserService),
/// секрет кластера — общий X-Internal-Secret.
/// </summary>
public class UserServiceClientSettings : HttpClientSettings
{
    public UserServiceClientSettings(IConfiguration configuration)
    {
        BaseAddress = configuration["Services:UserService"]
            ?? throw new InvalidOperationException("Не задан адрес user-service (Services:UserService)");

        DefaultHeaders = headers => headers.Add("X-Internal-Secret",
            configuration["Internal:Secret"] ?? string.Empty);
    }
}
