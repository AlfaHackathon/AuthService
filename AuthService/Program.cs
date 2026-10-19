using AlfaHackaton.Auth;
using AlfaHackaton.Controllers;
using AuthService.Common;

var builder = WebApplication.CreateBuilder(args);

// Настройки JWT и TokenBuilder из пакета AlfaHackaton.Auth
builder.Services
    .AddJwtSettings(builder.Configuration.GetSection("Jwt"))
    .AddTokenBuilder()
    .AddSingleton<AlfaHackaton.Auth.Common.Interfaces.ITokenValidator, AlfaHackaton.Auth.Tokens.TokenValidator>();

// Иначе JwtSecurityTokenHandler переименует claim роли (длинный URI -> "role"),
// а TokenParser у гейтвея ждёт именно длинный URI
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

// HttpClient для походов в user-service (с общим секретом кластера)
builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]
        ?? throw new InvalidOperationException("Не задан адрес user-service (Services:UserService)"));
    client.DefaultRequestHeaders.Add("X-Internal-Secret", builder.Configuration["Internal:Secret"] ?? string.Empty);
});

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
