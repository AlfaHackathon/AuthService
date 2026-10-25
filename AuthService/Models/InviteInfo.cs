namespace AuthService.Models;

/// <summary>Публичная проверка кода инвайта company-service: валиден ли и куда приглашает.</summary>
public class InviteInfo
{
    public bool Valid { get; set; }
    public string? CompanyName { get; set; }
    public string? Reason { get; set; }
    /// <summary>Какой HTTP-код отдавать клиенту, если инвайт невалиден (null — валиден).</summary>
    public int? StatusCode { get; set; }
}

/// <summary>Результат internal-использования инвайта (company-service).</summary>
/// <param name="Status">HTTP-статус ответа company-service.</param>
/// <param name="Error">Текст ошибки (тело ручки company-service), null — успех.</param>
/// <param name="CompanyId">Компания, к которой прикрепили юзера.</param>
public record UseInviteResult(int Status, string? Error, Guid CompanyId)
{
    public bool Success => Error is null;
}
