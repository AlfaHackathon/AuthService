using AlfaHackaton.Auth;
using AlfaHackaton.Controllers;
using AlfaHackaton.HttpClient;
using AuthService.Common;
using AuthService.Services;
using AuthService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Настройки JWT и TokenBuilder из пакета AlfaHackaton.Auth
builder.Services
    .AddJwtSettings(builder.Configuration.GetSection("Jwt"))
    .AddTokenBuilder()
    .AddSingleton<AlfaHackaton.Auth.Common.Interfaces.ITokenValidator, AlfaHackaton.Auth.Tokens.TokenValidator>();

// Иначе JwtSecurityTokenHandler переименует claim роли (длинный URI -> "role"),
// а TokenParser у гейтвея ждёт именно длинный URI
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

// Клиент user-service через AlfaHackaton.HttpClient:
// BaseAddress/секрет в UserServiceClientSettings, DI — через пакет
builder.Services.AddAlfaHttpClient(new UserServiceClientSettings(builder.Configuration));

builder.Services.AddControllers(options =>
{
    // Глобальный префикс auth-service для всех контроллеров
    options.Conventions.Add(new RoutePrefixConvention("auth-service"));
});

// Swagger с авторизацией через Bearer-токен
builder.Services.AddSwaggerAuth();

var app = builder.Build();

// Swagger: доступен на /auth-service/swagger, включается флагом Swagger:Enabled (из .env)
if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger(options => options.RouteTemplate = "auth-service/swagger/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/auth-service/swagger/v1/swagger.json", "Auth Service API");
        options.RoutePrefix = "auth-service/swagger";
    });
}

app.MapControllers();

app.Run();
