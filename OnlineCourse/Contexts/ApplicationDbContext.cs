using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Entities;

namespace OnlineCourse.Contexts;

public class ApplicationDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
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
    }
}