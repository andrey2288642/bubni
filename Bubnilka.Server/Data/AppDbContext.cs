using Microsoft.EntityFrameworkCore;
using Bubnilka.Shared.Models;

namespace Bubnilka.Server.Data;

/// <summary>
/// Контекст базы данных с защитой от SQL-инъекций через Entity Framework
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Индексы для производительности
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(128);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            // Уникальный индекс на пару пользователей (чтобы не создавать дубликаты чатов)
            entity.HasIndex(e => new { e.User1Id, e.User2Id }).IsUnique();
            
            entity.HasOne(e => e.User1)
                  .WithMany()
                  .HasForeignKey(e => e.User1Id)
                  .OnDelete(DeleteBehavior.Restrict); // Запрещаем каскадное удаление

            entity.HasOne(e => e.User2)
                  .WithMany()
                  .HasForeignKey(e => e.User2Id)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasOne(e => e.Chat)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.ChatId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                  .WithMany()
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
