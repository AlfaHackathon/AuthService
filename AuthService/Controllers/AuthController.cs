using AlfaHackaton.Auth.Common;
using AlfaHackaton.Auth.Common.Interfaces;
using AlfaHackaton.Auth.Tokens;
using AuthService.Common;
using AuthService.Models;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;

namespace AuthService.Controllers;

/// <summary>
/// Ручки авторизации.
/// Префикс auth-service добавляется глобально через RoutePrefixConvention.
/// </summary>
[ApiController]
public class AuthController : ControllerBase
{
    private const string TokenTypeClaim = "token_type";
    private const string AccessTokenType = "access";
    private const string RefreshTokenType = "refresh";

    private const int DefaultAccessTokenExpiry = 60 * 24 * 7;   // 7 дней
    private const int DefaultRefreshTokenExpiry = 60 * 24;      // 1 день (меньше access)

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenBuilder _tokenBuilder;
    private readonly ITokenValidator _tokenValidator;
    private readonly IConfiguration _configuration;

    public AuthController(IHttpClientFactory httpClientFactory, TokenBuilder tokenBuilder,
        ITokenValidator tokenValidator, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _tokenBuilder = tokenBuilder;
        _tokenValidator = tokenValidator;
        _configuration = configuration;
    }

    /// <summary>
    /// Логин по email и паролю: проверяет пользователя в user-service
    /// и возвращает пару access/refresh JWT-токенов.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByEmailAsync(request.Email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.HashedPassword))
        {
            return Unauthorized(new { error = "Неверный email или пароль" });
        }

        return Ok(BuildTokenPair(user.Id));
    }

    /// <summary>
    /// Регистрация: проверяет, что email свободен, создаёт пользователя в user-service
    /// и возвращает пару access/refresh JWT-токенов.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await EmailExistsAsync(request.Email, cancellationToken);
        if (exists is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "user-service недоступен" });
        }

        if (exists.Value)
        {
            return Conflict(new { error = $"Пользователь с email {request.Email} уже существует" });
        }

        var httpClient = _httpClientFactory.CreateClient("UserService");
        var createResponse = await httpClient.PostAsJsonAsync("user-service/internal/users", new CreateUserRequest(
            request.LastName, request.FirstName, request.MiddleName,
            request.Email, PasswordHasher.Hash(request.Password)), cancellationToken);

        if (!createResponse.IsSuccessStatusCode)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Не удалось создать пользователя" });
        }

        var user = await createResponse.Content.ReadFromJsonAsync<UserServiceUser>(cancellationToken);
        return Ok(BuildTokenPair(user!.Id));
    }

    /// <summary>
    /// Обновление пары токенов по refresh-токену: возвращает новую пару access/refresh.
    /// </summary>
    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        TokenPayload payload;
        try
        {
            payload = TokenParser.GetClaims(request.RefreshToken);
            _tokenValidator.Validate(payload);
        }
        catch (Exception exception)
            when (exception is AlfaHackaton.Auth.Exceptions.TokenInvalidException or ArgumentException or FormatException or System.Text.Json.JsonException)
        {
            return Unauthorized(new { error = "Невалидный или истёкший refresh-токен" });
        }

        // Выдать новую пару можно только по refresh-токену, access сюда не подходит
        var tokenType = ReadClaim(request.RefreshToken, TokenTypeClaim);
        if (tokenType != RefreshTokenType)
        {
            return Unauthorized(new { error = "Это не refresh-токен" });
        }

        return Ok(BuildTokenPair(payload.UserId));
    }

    private object BuildTokenPair(Guid userId)
    {
        return new
        {
            accessToken = BuildToken(userId, AccessTokenType, 
                _configuration.GetValue<int?>("Jwt:AccessTokenExpiryInMinutes") ?? DefaultAccessTokenExpiry),
            refreshToken = BuildToken(userId, RefreshTokenType,
                _configuration.GetValue<int?>("Jwt:RefreshTokenExpiryInMinutes") ?? DefaultRefreshTokenExpiry)
        };
    }

    private string BuildToken(Guid userId, string tokenType, int expiryMinutes)
    {
        return _tokenBuilder.Build(
        [
            new Claim("UserId", userId.ToString()),
            new Claim(System.Security.Claims.ClaimTypes.Role, UserRoles.User.ToString()),
            new Claim(TokenTypeClaim, tokenType)
        ],
        expiryMinutes);
    }

    /// <summary>Читает сырой claim из токена (TokenParser возвращает только строго типизированный payload).</summary>
    private static string? ReadClaim(string token, string claimType)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <returns>true/false — существует; null — user-service недоступен</returns>
    private async Task<bool?> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserService");
            var response = await client.GetAsync($"user-service/internal/users/exists-by-email/{email}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return (await response.Content.ReadFromJsonAsync<ExistsResponse>(cancellationToken))?.Exists;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task<UserServiceUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserService");
            var response = await client.GetAsync($"user-service/internal/users/by-email/{email}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserServiceUser>(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}

/// <summary>
/// Данные для логина.
/// </summary>
public record LoginRequest(string Email, string Password);

/// <summary>
/// Данные для регистрации.
/// </summary>
public record RegisterRequest(string Email, string Password, string LastName, string FirstName, string? MiddleName);

/// <summary>
/// Ответ на проверку существования email.
/// </summary>
public record ExistsResponse(bool Exists);

/// <summary>
/// Данные для создания пользователя (уходит в user-service).
/// </summary>
public record CreateUserRequest(string LastName, string FirstName, string? MiddleName, string Email, string HashedPassword);

/// <summary>
/// Refresh-токен для обновления пары.
/// </summary>
public record RefreshRequest(string RefreshToken);
