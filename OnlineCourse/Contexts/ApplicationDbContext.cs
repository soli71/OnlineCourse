using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineCourse.Blogs.Entities;
using OnlineCourse.Carts;
using OnlineCourse.Entities;
using OnlineCourse.Identity.Entities;
using OnlineCourse.Orders;
using OnlineCourse.Products.Entities;

namespace OnlineCourse.Contexts;

public class ApplicationDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
    public DbSet<Product> Products { get; set; }

    public DbSet<License> Licenses { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetails> OrderDetails { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<CourseSeason> CourseSeasons { get; set; }
    public DbSet<HeadLines> HeadLines { get; set; }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Province> Provinces { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<DiscountCode> DiscountCodes { get; set; }
    public DbSet<DiscountUsage> DiscountUsages { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<UserRole>().ToTable("UserRoles");
        modelBuilder.Entity<UserClaim>().ToTable("UserClaims");
        modelBuilder.Entity<UserLogin>().ToTable("UserLogins");
        modelBuilder.Entity<RoleClaim>().ToTable("RoleClaims");
        modelBuilder.Entity<UserToken>().ToTable("UserTokens");
        modelBuilder.Entity<SiteSetting>().HasData(new SiteSetting
        {
            AboutUs = "درباره ما",
            Address = "آدرس",
            Email = "ایمیل",
            FooterContent = "محتوای فوتر",
            Id = 1,
            Map = "نقشه",
            PhoneNumber = "شماره تلفن",
            PostalCode = "کد پستی"
        });
    }
}

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasIndex(builder => builder.Slug).IsUnique();
    }
}

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.HasIndex(builder => builder.Slug).IsUnique();
    }
}

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasDiscriminator<string>("ProductType")
               .HasValue<Course>("Course")
               .HasValue<PhysicalProduct>("PhysicalProduct");
    }
}

public class AuditInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
           DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context is ApplicationDbContext dbContext)
        {
            var entries = dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added && entry.Entity is ICreatedAudit auditableEntity)
                {
                    auditableEntity.CreatedAt = DateTime.UtcNow;

                    //auditableEntity.LastModifiedDate = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified && entry.Entity is IModifiedAudit modifiedEntity)
                {
                    modifiedEntity.ModifiedAt = DateTime.UtcNow;

                    //auditableEntity.LastModifiedDate = DateTime.UtcNow;
                }
            }
        }
    }
}