using System.ComponentModel.DataAnnotations;

/// <summary>Obuna tarifi (1 oy, 3 oy, 1 yil...). Narxni admin o'zgartiradi.</summary>
public class SubscriptionPlan
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = "";

    /// <summary>Obuna davomiyligi (oy).</summary>
    public int Months { get; set; }

    /// <summary>Narx, so'm (butun son).</summary>
    public long Price { get; set; }

    /// <summary>Ilovada ko'rsatiladigan qisqa belgi: "Eng ommabop", "Eng foydali".</summary>
    [MaxLength(64)]
    public string? Badge { get; set; }

    /// <summary>Nofaol tarif ilovada ko'rinmaydi va sotib olinmaydi.</summary>
    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bitta to'lov (buyurtma). Ilova obuna tanlaganda "pending" holatda yaratiladi; Click yoki Payme to'lovni
/// tasdiqlaganda "paid" bo'ladi va foydalanuvchining kirish muddati (AppUser.AccessExpiresAt) avtomatik uzayadi.
/// Admin to'lovsiz bergan muddat ham shu jadvalga Provider = "admin", Amount = 0 bilan yoziladi (tarix uchun).
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int? PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }

    public int Months { get; set; }
    public int Days { get; set; }

    /// <summary>To'lov summasi, so'm.</summary>
    public long Amount { get; set; }

    /// <summary>PaymentProviders.*</summary>
    [MaxLength(16)]
    public string Provider { get; set; } = "";

    /// <summary>PaymentStatus.*</summary>
    [MaxLength(16)]
    public string Status { get; set; } = PaymentStatus.Pending;

    /// <summary>To'lov tizimidagi tranzaksiya identifikatori (Payme "id", Click "click_trans_id").</summary>
    [MaxLength(64)]
    public string? ProviderTransId { get; set; }

    /// <summary>Payme tranzaksiya holati: 1 yaratilgan, 2 bajarilgan, -1 bekor (bajarilmasdan), -2 bekor (bajarilgandan keyin).</summary>
    public int ProviderState { get; set; }

    /// <summary>Payme tranzaksiyasi yaratilgan vaqt (Payme yuborgan, ms).</summary>
    public long ProviderCreateTime { get; set; }
    public long ProviderPerformTime { get; set; }
    public long ProviderCancelTime { get; set; }
    public int? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    /// <summary>To'lov qaysi davrni qopladi (muddat uzaytirilgandagi qiymatlar).</summary>
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    /// <summary>Admin berganda — qaysi admin.</summary>
    public int? GrantedByUserId { get; set; }
}

public static class PaymentStatus
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";
}

public static class PaymentProviders
{
    public const string Click = "click";
    public const string Payme = "payme";
    /// <summary>Faqat Payments:TestMode = true bo'lganda (haqiqiy pul o'tmaydi).</summary>
    public const string Test = "test";
    /// <summary>Admin to'lovsiz bergan muddat.</summary>
    public const string Admin = "admin";

    /// <summary>Pul to'lab kirganlar — admin panelda alohida ko'rsatiladi.</summary>
    public static readonly string[] Paid = { Click, Payme, Test };

    public static bool IsOnline(string? p) => p == Click || p == Payme || p == Test;
}

public static class RegisteredVia
{
    /// <summary>Foydalanuvchi o'zi mobil ilovada ro'yxatdan o'tgan.</summary>
    public const string App = "app";
}
