using AlfaHackaton.HttpClient.Common.Interfaces;
using AuthService.Contracts;
using AuthService.Controllers;
using AuthService.Models;
using AuthService.Services.Interfaces;

namespace AuthService.Services;

/// <summary>
/// Клиент user-service: ходит в другой сервис через AlfaHackaton.HttpClient.
/// BaseAddress и internal-секрет — из настроек (Services/UserServiceClientSettings.cs).
/// </summary>
public class UserServiceClient(IHttpClient<UserServiceClientSettings> http) : IUserServiceClient
{
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var response = await http.Get<ExistsResponse>($"user-service/internal/users/exists-by-email/{email}", cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode && response.Result?.Exists == true;
    }

    public async Task<UserServiceUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var response = await http.Get<UserServiceUser>($"user-service/internal/users/by-email/{Uri.EscapeDataString(email)}", cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode ? response.Result : null;
    }

    public async Task<UserServiceUser?> AddAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = await http.Post<CreateUserRequest, UserServiceUser>("user-service/internal/users", request, cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode ? response.Result : null;
    }
}
