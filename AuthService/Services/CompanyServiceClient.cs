using System.Text.Json;
using AlfaHackaton.HttpClient.Common.Interfaces;
using AuthService.Models;
using AuthService.Services.Interfaces;

namespace AuthService.Services;

/// <summary>
/// Клиент company-service: ходит в другой сервис через AlfaHackaton.HttpClient.
/// BaseAddress и internal-секрет — из настроек (Settings/CompanyServiceClientSettings.cs).
/// </summary>
public class CompanyServiceClient(IHttpClient<CompanyServiceClientSettings> http) : ICompanyServiceClient
{
    public async Task<InviteInfo?> ValidateInviteAsync(string code, CancellationToken cancellationToken = default)
    {
        var response = await http.Get<InviteInfo>($"company-service/internal/invites/{Uri.EscapeDataString(code)}", cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode ? response.Result : null;
    }

    public async Task<UseInviteResult> UseInviteAsync(string code, Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await http.Post<object, object>(
            $"company-service/internal/invites/{Uri.EscapeDataString(code)}/use",
            new { userId },
            cancellationToken: cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new UseInviteResult((int)response.Status, null, Guid.Empty);
        }

        return new UseInviteResult((int)response.Status, ReadError(response.TextData), Guid.Empty);
    }

    /// <summary>Тело ошибки вида {"error": "..."} -> текст; null, если разобрать не удалось.</summary>
    private static string? ReadError(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
