using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineCourse.Entities;
using System.Reflection.Emit;

namespace OnlineCourse.Contexts;

public class ApplicationDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
    public DbSet<Course> Courses { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetails> OrderDetails { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<CourseSeason> CourseSeasons { get; set; }
    public DbSet<HeadLines> HeadLines { get; set; }
    public DbSet<Blog> Blogs { get; set; }

    public DbSet<SiteSetting> SiteSettings { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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