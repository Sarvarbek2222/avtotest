using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using propro;

/// <summary>Foydalanuvchilarni boshqarish — faqat super admin.</summary>
[Authorize(Roles = Roles.SuperAdmin)]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserService _users;
    private readonly IStringLocalizer<SharedResource> _t;

    public UsersController(AppDbContext db, UserService users, IStringLocalizer<SharedResource> t)
    {
        _db = db;
        _users = users;
        _t = t;
    }

    private string T(string key) => _t[key];
    private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Users.AsNoTracking()
            .OrderByDescending(u => u.Role == Roles.SuperAdmin)
            .ThenBy(u => u.Username)
            .ToListAsync();

        ViewBag.CurrentUserId = CurrentUserId;
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new UserForm { Role = Roles.User, IsActive = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserForm form)
    {
        ModelState.Clear();
        await ValidateAsync(form, isNew: true);
        if (!ModelState.IsValid) return View("Form", form);

        var user = new AppUser
        {
            Username = form.Username!.Trim(),
            FullName = Clean(form.FullName),
            Role = form.Role!,
            IsActive = form.IsActive,
        };
        _users.SetPassword(user, form.Password!);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        TempData["MsgKey"] = "users.created";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        return View("Form", new UserForm
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserForm form)
    {
        var user = await _db.Users.FindAsync(form.Id);
        if (user == null) return NotFound();

        ModelState.Clear();
        await ValidateAsync(form, isNew: false);

        // Oxirgi faol super adminni pasaytirish/o'chirish mumkin emas; o'zini ham nofaol qilib bo'lmaydi
        bool losesAdmin = user.Role == Roles.SuperAdmin && user.IsActive &&
                          (form.Role != Roles.SuperAdmin || !form.IsActive);
        if (losesAdmin && await _users.ActiveSuperAdminCountAsync(exceptUserId: user.Id) == 0)
            ModelState.AddModelError("", T("err.lastSuperadmin"));
        if (user.Id == CurrentUserId && !form.IsActive)
            ModelState.AddModelError("", T("err.deactivateSelf"));

        if (!ModelState.IsValid) return View("Form", form);

        bool accessChanged = user.Role != form.Role || user.IsActive != form.IsActive;

        user.Username = form.Username!.Trim();
        user.FullName = Clean(form.FullName);
        user.Role = form.Role!;
        user.IsActive = form.IsActive;

        if (!string.IsNullOrEmpty(form.Password))
            _users.SetPassword(user, form.Password); // SecurityStamp ham yangilanadi
        else if (accessChanged)
            user.SecurityStamp = Guid.NewGuid().ToString("N"); // eski sessiyalar yangi huquqlarga o'tsin

        await _db.SaveChangesAsync();

        // O'z parolini/rolini o'zgartirgan admin tizimdan chiqib ketmasligi uchun cookie yangilanadi
        if (user.Id == CurrentUserId)
            await _users.SignInAsync(HttpContext, user);

        TempData["MsgKey"] = "users.updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            TempData["ErrKey"] = "err.userNotFound";
            return RedirectToAction(nameof(Index));
        }
        if (user.Id == CurrentUserId)
        {
            TempData["ErrKey"] = "err.deleteSelf";
            return RedirectToAction(nameof(Index));
        }
        if (user.Role == Roles.SuperAdmin && user.IsActive &&
            await _users.ActiveSuperAdminCountAsync(exceptUserId: user.Id) == 0)
        {
            TempData["ErrKey"] = "err.lastSuperadmin";
            return RedirectToAction(nameof(Index));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        TempData["MsgKey"] = "users.deleted";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(UserForm form, bool isNew)
    {
        form.Username = form.Username?.Trim();

        if (string.IsNullOrEmpty(form.Username))
            ModelState.AddModelError(nameof(form.Username), T("err.usernameRequired"));
        else if (!UserService.IsValidUsername(form.Username))
            ModelState.AddModelError(nameof(form.Username), T("err.usernameFormat"));
        else if (await _db.Users.AnyAsync(u => u.Username == form.Username && u.Id != form.Id))
            ModelState.AddModelError(nameof(form.Username), T("err.usernameTaken"));

        if (!Roles.IsValid(form.Role))
            ModelState.AddModelError(nameof(form.Role), T("err.roleInvalid"));

        if (isNew && string.IsNullOrEmpty(form.Password))
            ModelState.AddModelError(nameof(form.Password), T("err.passwordRequired"));
        if (!string.IsNullOrEmpty(form.Password))
        {
            if (form.Password.Length < UserService.MinPasswordLength)
                ModelState.AddModelError(nameof(form.Password), T("err.passwordShort"));
            else if (form.Password != form.ConfirmPassword)
                ModelState.AddModelError(nameof(form.ConfirmPassword), T("err.passwordMismatch"));
        }
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

/// <summary>Foydalanuvchi qo'shish/tahrirlash formasi.</summary>
public class UserForm
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }

    public bool IsNew => Id == 0;
}
