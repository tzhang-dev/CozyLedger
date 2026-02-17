using CozyLedger.Domain.Entities;
using CozyLedger.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for CozyLedger.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the set of books.
    /// </summary>
    public DbSet<Book> Books => Set<Book>();

    /// <summary>
    /// Gets the set of book invites.
    /// </summary>
    public DbSet<BookInvite> BookInvites => Set<BookInvite>();

    /// <summary>
    /// Gets the set of memberships.
    /// </summary>
    public DbSet<Membership> Memberships => Set<Membership>();

    /// <summary>
    /// Gets the set of accounts.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Gets the set of categories.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Gets the set of transactions.
    /// </summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>
    /// Gets the set of attachments.
    /// </summary>
    public DbSet<Attachment> Attachments => Set<Attachment>();

    /// <summary>
    /// Gets the set of tags.
    /// </summary>
    public DbSet<Tag> Tags => Set<Tag>();

    /// <summary>
    /// Gets the set of transaction-tag relationships.
    /// </summary>
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();

    /// <summary>
    /// Gets the set of exchange rates.
    /// </summary>
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    /// <summary>
    /// Configures entity mappings, constraints, and relationships.
    /// </summary>
    /// <param name="builder">Model builder used to configure EF mappings.</param>
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
