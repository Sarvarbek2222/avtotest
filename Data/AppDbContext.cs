using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Question> Questions { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<AppUser> Users { get; set; }
    public DbSet<TestAttempt> TestAttempts { get; set; }
    public DbSet<TestAnswer> TestAnswers { get; set; }
    public DbSet<UserMistake> UserMistakes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(b =>
        {
            b.ToTable("Users");
            b.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<TestAttempt>(b =>
        {
            b.ToTable("TestAttempts");
            b.HasIndex(a => a.AttemptKey).IsUnique();
            b.HasIndex(a => new { a.UserId, a.FinishedAt });
            b.Ignore(a => a.Percent);
            b.HasMany(a => a.Answers).WithOne(x => x.Attempt!).HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestAnswer>(b =>
        {
            b.ToTable("TestAnswers");
            b.HasIndex(x => x.QuestionId);
        });

        modelBuilder.Entity<UserMistake>(b =>
        {
            b.ToTable("UserMistakes");
            b.HasIndex(m => new { m.UserId, m.QuestionId }).IsUnique();
        });
    }
}
