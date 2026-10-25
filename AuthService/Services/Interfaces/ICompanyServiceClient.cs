using AuthService.Models;

namespace AuthService.Services.Interfaces;

/// <summary>Клиент company-service (internal-ручки, ходим по HTTP как сервис кластера).</summary>
public interface ICompanyServiceClient
{
    /// <summary>Публичная проверка кода инвайта, null — company-service недоступен/ответ не 200.</summary>
    Task<InviteInfo?> ValidateInviteAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Использовать инвайт: прикрепить юзера к компании и удалить инвайт.</summary>
    Task<UseInviteResult> UseInviteAsync(string code, Guid userId, CancellationToken cancellationToken = default);
}
