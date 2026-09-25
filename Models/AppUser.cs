using System.ComponentModel.DataAnnotations;

/// <summary>Tizim foydalanuvchisi. Parol ochiq holda saqlanmaydi — faqat PasswordHasher xeshi.</summary>
public class AppUser
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string Username { get; set; } = "";

    [MaxLength(128)]
    public string? FullName { get; set; }

    public string PasswordHash { get; set; } = "";

    /// <summary>Roles.SuperAdmin yoki Roles.User</summary>
    [MaxLength(32)]
    public string Role { get; set; } = Roles.User;

    /// <summary>Nofaol foydalanuvchi tizimga kira olmaydi.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Parol, rol yoki holat o'zgarganda yangilanadi — eski kirish cookie'lari bekor bo'ladi.</summary>
    [MaxLength(64)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}

public static class Roles
{
    public const string SuperAdmin = "superadmin";
    public const string User = "user";

    public static readonly string[] All = { SuperAdmin, User };

    public static bool IsValid(string? role) => role == SuperAdmin || role == User;
}
