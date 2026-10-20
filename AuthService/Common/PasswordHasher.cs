namespace AuthService.Common;

/// <summary>
/// Хэширование паролей (bcrypt). Так же хэшируются пароли в сиде user-service.
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool Verify(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            // хэш в базе не bcrypt-формата — считаем пароль неверным
            return false;
        }
    }
}
