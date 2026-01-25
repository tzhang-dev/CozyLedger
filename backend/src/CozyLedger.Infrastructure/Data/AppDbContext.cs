using CozyLedger.Domain.Entities;
using CozyLedger.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookInvite> BookInvites => Set<BookInvite>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Book>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BaseCurrency).HasMaxLength(3).IsRequired();
        });

        builder.Entity<BookInvite>(entity =>
        {
            entity.Property(e => e.Token).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.Book)
                .WithMany()
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Membership>(entity =>
        {
            entity.HasIndex(e => new { e.BookId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Book)
                .WithMany(b => b.Memberships)
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Account>(entity =>
        {
            entity.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameZhHans).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.HasOne(e => e.Book)
                .WithMany()
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(entity =>
        {
            entity.Property(e => e.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameZhHans).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Book)
                .WithMany()
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 4);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.HasOne(e => e.Book)
                .WithMany()
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ToAccount)
                .WithMany()
                .HasForeignKey(e => e.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Attachment>(entity =>
        {
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.MimeType).HasMaxLength(100).IsRequired();
            entity.HasOne(e => e.Transaction)
                .WithMany(t => t.Attachments)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.Property(e => e.NameEn).HasMaxLength(120).IsRequired();
            entity.Property(e => e.NameZhHans).HasMaxLength(120).IsRequired();
            entity.HasOne(e => e.Book)
                .WithMany()
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TransactionTag>(entity =>
        {
            entity.HasKey(e => new { e.TransactionId, e.TagId });
            entity.HasOne(e => e.Transaction)
                .WithMany(t => t.TransactionTags)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.TransactionTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ExchangeRate>(entity =>
        {
            entity.Property(e => e.BaseCurrency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.QuoteCurrency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Rate).HasPrecision(18, 6);
            entity.Property(e => e.Source).HasMaxLength(120).IsRequired();
        });
    }
}
